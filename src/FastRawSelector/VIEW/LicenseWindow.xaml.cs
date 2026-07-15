using FastRawSelector.LOGIC;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastRawSelector.VIEW
{
    /// <summary>
    /// 사용 라이브러리·네이티브 구성요소와 라이선스 전문 안내 (모달).
    /// HelpWindow 와 동일: code-behind 로 텍스트 세팅, DataContext 바인딩 없음.
    /// </summary>
    public partial class LicenseWindow : Window
    {
        public LicenseWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = Loc.Get("LicenseTitle");
            IntroTb.Text = Loc.Get("LicenseIntro");
            CloseBt.Content = Loc.Get("Close");

            string appVer = Common.GetVersion(Assembly.GetExecutingAssembly(), "", 3);
            LibList.Items.Clear();
            AddLib("FastRawSelector " + appVer, "GNU GPL v3", LicenseText.GplV3);
            AddLib("MaterialDesignThemes 4.7.1", "MIT", LicenseText.Mit);
            AddLib("MaterialDesignColors", "MIT", LicenseText.Mit);
            AddLib("MahApps.Metro.IconPacks.BoxIcons 4.11.0", "MIT", LicenseText.Mit);
            AddLib("MetadataExtractor 2.7.2", "Apache-2.0", LicenseText.Apache20);
            AddLib("XmpCore", "Apache-2.0", LicenseText.Apache20);
            AddLib("YamlDotNet 13.0.0", "MIT", LicenseText.Mit);
            AddLib("log4net 2.0.15", "Apache-2.0", LicenseText.Apache20);
            AddLib("WindowsAPICodePack-Shell 1.1.1", "Microsoft-derived", LicenseText.MsDerived);
            AddLib("Microsoft.Xaml.Behaviors.Wpf", "MIT", LicenseText.Mit);
            AddLib("Costura.Fody 3.3.3", "MIT", LicenseText.Mit);
            AddLib("Fody 4.2.1", "MIT", LicenseText.Mit);
            AddLib("System.ValueTuple 4.5.0", "MIT", LicenseText.Mit);
            AddLib("LibRaw (libraw.dll)", "LGPL-2.1 or CDDL", LicenseText.Lgpl21);
            AddLib("Exiv2 (exiv2-ql)", "GPL-2.0-or-later", LicenseText.GplV2Note);

            if (LibList.Items.Count > 0)
            {
                LibList.SelectedIndex = 0;
            }
        }

        private void AddLib(string name, string license, string body)
        {
            var item = new ListBoxItem
            {
                Content = name,
                Tag = new string[] { name, license, body },
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 13
            };
            LibList.Items.Add(item);
        }

        private void LibList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = LibList.SelectedItem as ListBoxItem;
            if (item == null)
            {
                return;
            }
            var parts = item.Tag as string[];
            if (parts == null || parts.Length < 3)
            {
                return;
            }
            DetailNameTb.Text = parts[0];
            DetailLicenseTb.Text = parts[1];
            DetailBodyTb.Text = parts[2];
        }

        private void CloseBt_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        /// <summary>라이선스 전문 (프로젝트 규칙: 바인딩 없이 code-behind 정적 텍스트).</summary>
        private static class LicenseText
        {
            public const string Mit =
@"MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.";

            public const string Apache20 =
@"Apache License
Version 2.0, January 2004
http://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.
""License"" shall mean the terms and conditions for use, reproduction,
and distribution as defined by Sections 1 through 9 of this document.

""Licensor"" shall mean the copyright owner or entity authorized by
the copyright owner that is granting the License.

""Legal Entity"" shall mean the union of the acting entity and all
other entities that control, are controlled by, or are under common
control with that entity.

""You"" (or ""Your"") shall mean an individual or Legal Entity
exercising permissions granted by this License.

""Source"" form shall mean the preferred form for making modifications,
including but not limited to software source code, documentation
source, and configuration files.

""Object"" form shall mean any form resulting from mechanical
transformation or translation of a Source form, including but
not limited to compiled object code, generated documentation,
and conversions to other media types.

""Work"" shall mean the work of authorship, whether in Source or
Object form, made available under the License, as indicated by a
copyright notice that is included in or attached to the work.

""Derivative Works"" shall mean any work, whether in Source or Object
form, that is based on (or derived from) the Work and for which the
editorial revisions, annotations, elaborations, or other modifications
represent, as a whole, an original work of authorship.

""Contribution"" shall mean any work of authorship, including
the original version of the Work and any modifications or additions
to that Work or Derivative Works thereof, that is intentionally
submitted to Licensor for inclusion in the Work by the copyright owner
or by an individual or Legal Entity authorized to submit on behalf of
the copyright owner.

""Contributor"" shall mean Licensor and any individual or Legal Entity
on behalf of whom a Contribution has been received by Licensor and
subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
this License, each Contributor hereby grants to You a perpetual,
worldwide, non-exclusive, no-charge, royalty-free, irrevocable
copyright license to reproduce, prepare Derivative Works of,
publicly display, publicly perform, sublicense, and distribute the
Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
this License, each Contributor hereby grants to You a perpetual,
worldwide, non-exclusive, no-charge, royalty-free, irrevocable
(except as stated in this section) patent license to make, have made,
use, offer to sell, sell, import, and otherwise transfer the Work.

4. Redistribution. You may reproduce and distribute copies of the
Work or Derivative Works thereof in any medium, with or without
modifications, and in Source or Object form, provided that You
meet the following conditions:

(a) You must give any other recipients of the Work or
    Derivative Works a copy of this License; and

(b) You must cause any modified files to carry prominent notices
    stating that You changed the files; and

(c) You must retain, in the Source form of any Derivative Works
    that You distribute, all copyright, patent, trademark, and
    attribution notices from the Source form of the Work; and

(d) If the Work includes a ""NOTICE"" text file as part of its
    distribution, then any Derivative Works that You distribute must
    include a readable copy of the attribution notices contained
    within such NOTICE file.

5. Submission of Contributions. Unless You explicitly state otherwise,
any Contribution intentionally submitted for inclusion in the Work
by You to the Licensor shall be under the terms and conditions of
this License, without any additional terms or conditions.

6. Trademarks. This License does not grant permission to use the trade
names, trademarks, service marks, or product names of the Licensor,
except as required for reasonable and customary use in describing the
origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
agreed to in writing, Licensor provides the Work (and each
Contributor provides its Contributions) on an ""AS IS"" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied, including, without limitation, any warranties or conditions
of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
PARTICULAR PURPOSE. You are solely responsible for determining the
appropriateness of using or redistributing the Work and assume any
risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
whether in tort (including negligence), contract, or otherwise,
unless required by applicable law (such as deliberate and grossly
negligent acts) or agreed to in writing, shall any Contributor be
liable to You for damages, including any direct, indirect, special,
incidental, or consequential damages of any character arising as a
result of this License or out of the use or inability to use the
Work.

9. Accepting Warranty or Additional Liability. While redistributing
the Work or Derivative Works thereof, You may choose to offer,
and charge a fee for, acceptance of support, warranty, indemnity,
or other liability obligations and/or rights consistent with this
License.

END OF TERMS AND CONDITIONS

Full text: https://www.apache.org/licenses/LICENSE-2.0";

            public const string GplV3 =
@"GNU GENERAL PUBLIC LICENSE
Version 3, 29 June 2007

Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
Everyone is permitted to copy and distribute verbatim copies
of this license document, but changing it is not allowed.

Preamble

The GNU General Public License is a free, copyleft license for
software and other kinds of works.

The licenses for most software and other practical works are designed
to take away your freedom to share and change the works. By contrast,
the GNU General Public License is intended to guarantee your freedom to
share and change all versions of a program--to make sure it remains free
software for all its users.

TERMS AND CONDITIONS (summary of key points)

0. Definitions.
""This License"" refers to version 3 of the GNU General Public License.
""The Program"" refers to any copyrightable work licensed under this
License. Each licensee is addressed as ""you"".

1. Source Code.
The ""source code"" for a work means the preferred form of the work
for making modifications to it. ""Object code"" means any non-source
form of a work.

2. Basic Permissions.
All rights granted under this License are granted for the term of
copyright on the Program, and are irrevocable provided the stated
conditions are met. You may make, run and propagate covered works
that you do not convey, without conditions so long as your license
otherwise remains in force.

3. Protecting Users' Legal Rights From Anti-Circumvention Law.
No covered work shall be deemed part of an effective technological
measure under any applicable law fulfilling obligations under article
11 of the WIPO copyright treaty.

4. Conveying Verbatim Copies.
You may convey verbatim copies of the Program's source code as you
receive it, in any medium, provided that you conspicuously and
appropriately publish on each copy an appropriate copyright notice;
keep intact all notices stating that this License and any
non-permissive terms added in accord with section 7 apply to the code;
keep intact all notices of the absence of any warranty; and give all
recipients a copy of this License along with the Program.

5. Conveying Modified Source Versions.
You may convey a work based on the Program, or the modifications to
produce it from the Program, in the form of source code under the
terms of section 4, provided that you also meet all of these conditions:
  a) The work must carry prominent notices stating that you modified
     it, and giving a relevant date.
  b) The work must carry prominent notices stating that it is
     released under this License.
  c) You must license the entire work, as a whole, under this License
     to anyone who comes into possession of a copy.
  d) If the work has interactive user interfaces, each must display
     Appropriate Legal Notices.

6. Conveying Non-Source Forms.
You may convey a covered work in object code form under the terms
of sections 4 and 5, provided that you also convey the
machine-readable Corresponding Source under the terms of this License,
in one of the ways specified in the full license text (e.g. accompany
with source, written offer, network access).

7. Additional Terms.
Additional permissions may be allowed as stated in the full text.

8–12. Termination, Acceptance, Automatic Licensing of Downstream
Recipients, Patents, No Surrender of Others' Freedom, etc.
See the full license text.

15. Disclaimer of Warranty.
THERE IS NO WARRANTY FOR THE PROGRAM, TO THE EXTENT PERMITTED BY
APPLICABLE LAW. EXCEPT WHEN OTHERWISE STATED IN WRITING THE COPYRIGHT
HOLDERS AND/OR OTHER PARTIES PROVIDE THE PROGRAM ""AS IS"" WITHOUT WARRANTY
OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO,
THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
PURPOSE.

16. Limitation of Liability.
IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING
WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MODIFIES AND/OR CONVEYS
THE PROGRAM AS PERMITTED ABOVE, BE LIABLE TO YOU FOR DAMAGES.

END OF TERMS AND CONDITIONS

FastRawSelector is licensed under the GNU GPL version 3.
Full official text: repository LICENSE file
https://www.gnu.org/licenses/gpl-3.0.html";

            /// <summary>Exiv2 등 upstream 이 GPL-2.0-or-later 인 경우 안내 (결합 작품은 앱 GPL-3 에 맞춤).</summary>
            public const string GplV2Note =
@"GNU GENERAL PUBLIC LICENSE
Version 2 (or later, at your option) — as used by some upstream components

This component (e.g. Exiv2 / related bindings) is available under
GPL-2.0-or-later from upstream. When combined into FastRawSelector,
the combined work is distributed under GNU GPL version 3
(see the FastRawSelector entry and the repository LICENSE file).

Key GPL freedoms (all versions):
- Freedom to run the program for any purpose
- Freedom to study and modify the source
- Freedom to redistribute copies
- Freedom to distribute modified versions under the same license

Full GPL-2 text:
https://www.gnu.org/licenses/old-licenses/gpl-2.0.html

Full GPL-3 text (this application):
https://www.gnu.org/licenses/gpl-3.0.html
Also see the LICENSE file in the FastRawSelector repository.";

            public const string Lgpl21 =
@"GNU LESSER GENERAL PUBLIC LICENSE
Version 2.1, February 1999

Copyright (C) 1991, 1999 Free Software Foundation, Inc.
51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
Everyone is permitted to copy and distribute verbatim copies
of this license document, but changing it is not allowed.

[This is the first released version of the Lesser GPL. It also counts
as the successor of the GNU Library Public License, version 2, hence
the version number 2.1.]

Preamble

The licenses for most software are designed to take away your
freedom to share and change it. By contrast, the GNU General Public
Licenses are intended to guarantee your freedom to share and change
free software--to make sure the software is free for all its users.

This license, the Lesser General Public License, applies to some
specially designated software packages--typically libraries--of the
Free Software Foundation and other authors who decide to use it.

TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

0. This License Agreement applies to any software library or other
program which contains a notice placed by the copyright holder or
other authorized party saying it may be distributed under the terms of
this Lesser General Public License (also called ""this License"").
Each licensee is addressed as ""you"".

A ""library"" means a collection of software functions and/or data
prepared so as to be conveniently linked with application programs
(which use some of those functions and data) to form executables.

1. You may copy and distribute verbatim copies of the Library's
complete source code as you receive it, in any medium, provided that
you conspicuously and appropriately publish on each copy an
appropriate copyright notice and disclaimer of warranty; keep intact
all the notices that refer to this License and to the absence of any
warranty; and distribute a copy of this License along with the Library.

2. You may modify your copy or copies of the Library or any portion
of it, thus forming a work based on the Library, and copy and
distribute such modifications or work under the terms of Section 1
above, provided that you also meet all of these conditions:

  a) The modified work must itself be a software library.

  b) You must cause the files modified to carry prominent notices
  stating that you changed the files and the date of any change.

  c) You must cause the whole of the work to be licensed at no
  charge to all third parties under the terms of this License.

  d) If a facility in the modified Library refers to a function or a
  table of data to be supplied by an application program that uses
  the facility, other than as an argument passed when the facility
  is invoked, then you must make a good faith effort to ensure that,
  in the event an application does not supply such function or
  table, the facility still operates, and performs whatever part of
  its purpose remains meaningful.

3. You may opt to apply the terms of the ordinary GNU General Public
License instead of this License to a given copy of the Library.

4. You may copy and distribute the Library (or a portion or
derivative of it, under Section 2) in object code or executable form
under the terms of Sections 1 and 2 above provided that you accompany
it with the complete corresponding machine-readable source code.

5. A program that contains no derivative of any portion of the
Library, but is designed to work with the Library by being compiled or
linked with it, is called a ""work that uses the Library"".

6. As an exception to the Sections above, you may also combine or
link a ""work that uses the Library"" with the Library to produce a
work containing portions of the Library, and distribute that work
under terms of your choice, provided that the terms permit
modification of the work for the customer's own use and reverse
engineering for debugging such modifications.

NO WARRANTY

15. BECAUSE THE LIBRARY IS LICENSED FREE OF CHARGE, THERE IS NO
WARRANTY FOR THE LIBRARY, TO THE EXTENT PERMITTED BY APPLICABLE LAW.

16. IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN
WRITING WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MAY MODIFY
AND/OR REDISTRIBUTE THE LIBRARY AS PERMITTED ABOVE, BE LIABLE TO YOU
FOR DAMAGES.

END OF TERMS AND CONDITIONS

LibRaw is dual-licensed under LGPL-2.1 or CDDL.
Full text: https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html
Project: https://www.libraw.org/";

            public const string MsDerived =
@"Windows API Code Pack for Microsoft .NET Framework

This software includes components derived from the Microsoft Windows API
Code Pack samples / community packages (WindowsAPICodePack-Shell).

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED ""AS IS"" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

Original project (historical):
https://github.com/aybe/Windows-API-Code-Pack-1.1

This application uses the NuGet package WindowsAPICodePack-Shell 1.1.1
for folder picker and shell integration APIs.";
        }
    }
}
