#!/bin/bash
cd "${0%/*}"
mkdir DestrospeanCASPEditor
cd ../DestrospeanCASPEditor/bin/Release
mkbundle --simple --static -o ../../../tools/DestrospeanCASPEditor/DestrospeanCASPEditor --nodeps DestrospeanCASPEditor.exe Destrospean.CmarNYCBorrowed.dll Destrospean.S3PIExtensions.dll GDImageLibrary.dll GLWidget.dll Newtonsoft.Json.dll OpenTK.dll System.Custom.dll TeximpNet.dll /usr/lib/mono/4.5/System.dll /usr/lib/mono/4.5/System.Configuration.dll /usr/lib/mono/4.5/System.Xml.dll /usr/lib/mono/4.5/System.Security.dll /usr/lib/mono/4.5/System.Numerics.dll /usr/lib/mono/4.5/System.Core.dll /usr/lib/mono/4.5/Mono.Security.dll /usr/lib/mono/4.5/mscorlib.dll /usr/lib/mono/gtk-sharp-2.0/gtk-sharp.dll /usr/lib/mono/gtk-sharp-2.0/glib-sharp.dll /usr/lib/mono/gtk-sharp-2.0/gdk-sharp.dll /usr/lib/mono/4.5/Mono.Cairo.dll /usr/lib/mono/gtk-sharp-2.0/pango-sharp.dll /usr/lib/mono/gtk-sharp-2.0/atk-sharp.dll /usr/lib/mono/4.5/System.Windows.Forms.dll /usr/lib/mono/4.5/System.Drawing.dll /usr/lib/mono/4.5/Mono.WebBrowser.dll /usr/lib/mono/4.5/System.Runtime.Serialization.Formatters.Soap.dll /usr/lib/mono/4.5/Accessibility.dll /usr/lib/mono/4.5/System.Data.dll /usr/lib/mono/4.5/System.Transactions.dll /usr/lib/mono/4.5/System.EnterpriseServices.dll /usr/lib/mono/4.5/System.Xml.Linq.dll /usr/lib/mono/4.5/System.Runtime.Serialization.dll /usr/lib/mono/4.5/System.ServiceModel.Internals.dll /usr/lib/mono/4.5/I18N.West.dll /usr/lib/mono/4.5/I18N.dll -L /usr/lib/mono/4.5/ -L /usr/lib/mono/gtk-sharp-2.0/ --machine-config /etc/mono/4.5/machine.config --config ../../../tools/config --library /usr/lib64/libglib-2.0.so.0 --library /usr/lib64/libgobject-2.0.so.0 --library /usr/lib64/libdatrie.so.1 --library /usr/lib64/libthai.so.0 --library /usr/lib64/libpango-1.0.so.0 --library /usr/lib64/libatk-1.0.so.0 --library /usr/lib64/libpixman-1.so.0 --library /usr/lib64/libz.so.1 --library /usr/lib64/libpng16.so.16 --library /usr/lib64/libfreetype.so.6 --library /usr/lib64/libexpat.so.1 --library /usr/lib64/libfontconfig.so.1 --library /usr/lib64/libXau.so.6 --library /usr/lib64/libbsd.so.0 --library /usr/lib64/libXdmcp.so.6 --library /usr/lib64/libxcb.so.1 --library /usr/lib64/libxcb-shm.so.0 --library /usr/lib64/libxcb-render.so.0 --library /usr/lib64/libX11.so.6 --library /usr/lib64/libXrender.so.1 --library /usr/lib64/libXext.so.6 --library /usr/lib64/libcairo.so.2 --library /usr/lib64/libgraphite2.so.3 --library /usr/lib64/libharfbuzz.so.0 --library /usr/lib64/libpangoft2-1.0.so.0 --library /usr/lib64/libpangocairo-1.0.so.0 --library /usr/lib64/libgmodule-2.0.so.0 --library /usr/lib64/libgio-2.0.so.0 --library /usr/lib64/libgdk_pixbuf-2.0.so.0 --library /usr/lib64/libXinerama.so.1 --library /usr/lib64/libXi.so.6 --library /usr/lib64/libXrandr.so.2 --library /usr/lib64/libXfixes.so.3 --library /usr/lib64/libXcursor.so.1 --library /usr/lib64/libXcomposite.so.1 --library /usr/lib64/libXdamage.so.1 --library /usr/lib64/libgdk-x11-2.0.so.0 --library /usr/lib64/libgtk-x11-2.0.so.0 --library /usr/lib64/libgtksharpglue-2.so --library /usr/lib64/libglibsharpglue-2.so --library /usr/lib64/libMonoPosixHelper.so --library /usr/lib64/libgdksharpglue-2.so --library /usr/lib64/libmono-native.so --library /usr/lib64/libc.so.6
rm DestrospeanCASPEditor-*.rar
cd ../../../tools
cp ../DestrospeanCASPEditor/bin/Release/Acknowledgements.txt DestrospeanCASPEditor
cp ../DestrospeanCASPEditor/bin/Release/GameFolders.xml DestrospeanCASPEditor
cp ../DestrospeanCASPEditor/bin/Release/LICENSE.md DestrospeanCASPEditor
cp ../DestrospeanCASPEditor/bin/Release/Mono.Posix.dll DestrospeanCASPEditor
cp ../DestrospeanCASPEditor/bin/Release/s3pi* DestrospeanCASPEditor
cp ../DestrospeanCASPEditor/Icons/DestrospeanCASPEditor.svg DestrospeanCASPEditor
rar a DestrospeanCASPEditor-fedora-amd64.rar DestrospeanCASPEditor/*
rm DestrospeanCASPEditor/*
cp ../DestrospeanCASPEditor/bin/Release/* DestrospeanCASPEditor
rm DestrospeanCASPEditor/*.log
rar a DestrospeanCASPEditor-win32-i386.rar DestrospeanCASPEditor/*
mv DestrospeanCASPEditor-*.rar ../DestrospeanCASPEditor/bin/Release
rm -rf DestrospeanCASPEditor
distrobox enter debian-bookworm -- ./bundle_debian.sh
