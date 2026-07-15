// Copyright © 2020 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

//using ImageMagick;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Xml;
using static System.Runtime.InteropServices.Marshal;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// exiv2-ql 네이티브 DLL 로 RAW EXIF·썸네일·방향·사이즈를 읽는다.
    /// 사이즈 EXIF 키가 없으면 libraw open + get_iwidth/iheight 폴백 (demosaic 없음, P-5).
    /// 출처: QuickLook MetaProvider (GPL-3.0 헤더 유지).
    /// </summary>
    public class MetaProvider
    {
        private readonly SortedDictionary<string, (string, string)> exifDic =
            new SortedDictionary<string, (string, string)>(); // [key, [label, value]]

        private readonly string _path;

        public MetaProvider(string path)
        {
            _path = path;
            // P-1: 생성자에서 EXIF 전체 파싱하지 않음. GetExif()/GetSize() 시 lazy.
        }

        /// <summary>EXIF XML 을 파싱해 라벨/값 사전으로 캐시. 첫 호출 시에만 네이티브 I/O.</summary>
        public SortedDictionary<string, (string, string)> GetExif()
        {
            if (exifDic.Count != 0)
                return exifDic;

            var exif = NativeMethods.GetExif(_path);
            if (string.IsNullOrEmpty(exif))
                return exifDic;

            var xml = new XmlDocument();
            xml.LoadXml(exif);
            var iter = xml.SelectNodes("/Exif/child::node()")?.GetEnumerator();
            while (iter != null && iter.MoveNext())
            {
                if (!(iter.Current is XmlNode node))
                    continue;

                var key = node.Name;
                var label = node.Attributes?["Label"]?.InnerText;
                var value = node.InnerText;

                if (!exifDic.ContainsKey(key))
                {
                    exifDic.Add(key, (label, value));
                }
            }

            return exifDic;
        }

        /// <summary>임베디드 썸네일 바이트. 없으면 빈 배열.</summary>
        public byte[] GetThumbnail()
        {
            return NativeMethods.GetThumbnail(_path) ?? new byte[0];
        }

        /// <summary>
        /// 이미지 전체 크기. 회전 보정(RotateAndScale)에 원본 종횡비가 필요하므로 썸네일 픽셀과 별개.
        /// allowExifLookup=true: 캐시된 EXIF _.Size 우선, 없으면 libraw.
        /// allowExifLookup=false: EXIF XML 로드 없이 libraw open + 사이즈만 (프리패치 고속).
        /// </summary>
        public Size GetSize(bool allowExifLookup = true)
        {
            int w = 0;
            int h = 0;

            if (allowExifLookup)
            {
                if (exifDic.Count == 0)
                {
                    GetExif();
                }
                exifDic.TryGetValue("_.Size.Width", out var w_);
                exifDic.TryGetValue("_.Size.Height", out var h_);
                if (w_.Item2 != null && h_.Item2 != null
                    && int.TryParse(w_.Item2, out w) && int.TryParse(h_.Item2, out h)
                    && w > 0 && h > 0)
                {
                    return new Size(w, h);
                }
            }

            // P-5: demosaic 없이 open 후 사이즈 메타만.
            // 회전 판별에는 센서(raw) 크기가 안전 — iwidth 는 버전/플립에 따라 달라질 수 있음.
            IntPtr handler = IntPtr.Zero;
            try
            {
                handler = RAWLib.libraw_init(RAWLib.LibRaw_init_flags.LIBRAW_OPTIONS_NONE);
                if (handler == IntPtr.Zero)
                {
                    return new Size(800, 600);
                }

                var err = RAWLib.libraw_open_wfile(handler, _path);
                if (err != RAWLib.LibRaw_errors.LIBRAW_SUCCESS)
                {
                    return new Size(800, 600);
                }

                // 미처리 센서 크기 우선 (Orientation 적용 전)
                w = RAWLib.libraw_get_raw_width(handler);
                h = RAWLib.libraw_get_raw_height(handler);
                if (w <= 0 || h <= 0)
                {
                    try
                    {
                        RAWLib.libraw_adjust_sizes_info_only(handler);
                    }
                    catch
                    {
                        // ignore
                    }
                    w = RAWLib.libraw_get_iwidth(handler);
                    h = RAWLib.libraw_get_iheight(handler);
                }

                return (w <= 0 || h <= 0) ? new Size(800, 600) : new Size(w, h);
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(_path, ex);
                return new Size(800, 600);
            }
            finally
            {
                if (handler != IntPtr.Zero)
                {
                    RAWLib.libraw_close(handler);
                }
            }
        }

        /// <summary>EXIF Orientation (1~8). 실패 시 Undefined(0).</summary>
        public Orientation GetOrientation()
        {
            return (Orientation)NativeMethods.GetOrientation(_path);
        }
    }

    /// <summary>exiv2-ql-32/64 프로세스 비트에 맞춰 DllImport. 두 패스(길이 조회 후 버퍼 채움).</summary>
    internal static class NativeMethods
    {
        private static readonly bool Is64 = Environment.Is64BitProcess;

        public static string GetExif(string file)
        {
            try
            {
                var len = Is64 ? GetExif_64(file, null) : GetExif_32(file, null);
                if (len <= 0)
                    return string.Empty;

                var sb = new StringBuilder(len + 1);
                var _ = Is64 ? GetExif_64(file, sb) : GetExif_32(file, sb);

                return sb.ToString();
            }
            catch (Exception e)
            {
                Log.ExceptionWithMsg(file, e);
                return string.Empty;
            }
        }

        public static byte[] GetThumbnail(string file)
        {
            try
            {
                var len = Is64 ? GetThumbnail_64(file, null) : GetThumbnail_32(file, null);
                if (len <= 0)
                    return null;

                var buffer = new byte[len];
                var _ = Is64 ? GetThumbnail_64(file, buffer) : GetThumbnail_32(file, buffer);

                return buffer;
            }
            catch (Exception e)
            {
                Log.ExceptionWithMsg(file, e);
                return null;
            }
        }

        public static int GetOrientation(string file)
        {
            try
            {
                return Is64 ? GetOrientation_64(file) : GetOrientation_32(file);
            }
            catch (Exception e)
            {
                Log.ExceptionWithMsg(file, e);
                return 0;
            }
        }

        [DllImport("exiv2-ql-32.dll", EntryPoint = "GetExif", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetExif_32([MarshalAs(UnmanagedType.LPWStr)] string file,
            [MarshalAs(UnmanagedType.LPStr)] StringBuilder sb);

        [DllImport("exiv2-ql-32.dll", EntryPoint = "GetThumbnail", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetThumbnail_32([MarshalAs(UnmanagedType.LPWStr)] string file,
            [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);

        [DllImport("exiv2-ql-32.dll", EntryPoint = "GetOrientation", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetOrientation_32([MarshalAs(UnmanagedType.LPWStr)] string file);

        [DllImport("exiv2-ql-64.dll", EntryPoint = "GetExif", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetExif_64([MarshalAs(UnmanagedType.LPWStr)] string file,
            [MarshalAs(UnmanagedType.LPStr)] StringBuilder sb);

        [DllImport("exiv2-ql-64.dll", EntryPoint = "GetThumbnail", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetThumbnail_64([MarshalAs(UnmanagedType.LPWStr)] string file,
            [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);

        [DllImport("exiv2-ql-64.dll", EntryPoint = "GetOrientation", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetOrientation_64([MarshalAs(UnmanagedType.LPWStr)] string file);
    }

    public enum Orientation
    {
        Undefined = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 3,
        BottomLeft = 4,
        LeftTop = 5,
        RightTop = 6,
        RightBottom = 7,
        LeftBottom = 8
    }
}