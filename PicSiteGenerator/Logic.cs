using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace PicSiteGenerator
{
    public class Logic
    {
        static void Main2(string[] args)
        {
            string p = @"E:\Program Files\Classified files\";
            string r = @"e:\testsite\";

            p = @"\\readyshare\USB_Storage\no see\sese";
            r = @"\\readyshare\USB_Storage\html\";

            p = p.ToLower().TrimEnd('/', '\\');
            r = r.ToLower().TrimEnd('/', '\\');

            //var x1= "d:\\tddownload\\h\\College.Rules.Site.Rip.HD.XXX.Pack.720p-sFZ";
            //var x2 = "d:\\testsite\\College.Rules.Site.Rip.HD.XXX.Pack.720p-sFZ";
            //var r1= MakeRelativePath(x1, x2);
            //var r2 = MakeRelativePath(x1 + '\\', x2 + '\\');

            Traverse(p, r, 2);
            GenerateIndex("Picture Index", r);
            //GenerateHtmlForFolder(p, r);
            //p = @"C:\Users\Administrator\Pictures\temp1\";
        }

        internal static int PageSize { set; get; }
        internal static void Traverse(string parentpath, string htmlparentpath, int level)
        {
            if (level <= 0) return;

            var subdirs = Directory.GetDirectories(parentpath);
            foreach (var dir in subdirs)
            {
                var dirname = dir.Substring(dir.LastIndexOf('\\'));
                var htmlpath = htmlparentpath + dirname;
                Traverse(dir, htmlpath, level - 1);
            }

            GenerateHtmlForFolder(parentpath, htmlparentpath, PageSize);
        }

        internal static void GenerateIndex(string title, string htmlrootpath)
        {
            var listdiv = new XElement("div");

            var builder = new StringBuilder();
            AppendWithEncode(builder, "<ul>");
            TraverseIndex(htmlrootpath, htmlrootpath, builder);
            AppendWithEncode(builder, "</ul>");
            listdiv.Add(XElement.Parse(builder.ToString()).Element("li").Element("ul"));

            var htmldoc = new XDocument(
                    new XElement("html",
                        new XElement("header",
                            new XElement("title", title),
                            new XElement("meta",
                                new XAttribute("charset", "UTF-8"))),
                        new XElement("body",
                            new XElement("p", new XElement("h1", title)),
                            listdiv)
                ));

            SaveToFile(htmldoc, htmlrootpath + "\\index.html");
        }

        internal static void AppendWithEncode(StringBuilder sb, string text, params object[] ps)
        {
            var psencode = new string[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                psencode[i] = HttpUtility.HtmlEncode(ps[i]);
            }
            if (ps.Length == 0)
                sb.Append(text);
            else
                sb.Append(string.Format(text, psencode));
        }

        static void TraverseIndex(string htmlpath, string roothtmlpath, StringBuilder builder)
        {
            var foldername = HttpUtility.HtmlEncode(
                htmlpath.Substring(htmlpath.LastIndexOf('\\') + 1));
            
            // 
            AppendWithEncode(builder, "<li>");

            // image
            var imgfiles = Directory.EnumerateFiles(htmlpath, "cover.*");
            if (imgfiles.Count() > 0)
            {
                var repath = MakeRelativePath(imgfiles.First(), roothtmlpath);
                AppendWithEncode(builder, "<img src='{0}' width='100' heigh='100'/>", repath);
            }

            // name
            AppendWithEncode(builder, foldername);

            // page link
            int htmlidx = 0;
            var htmlfiles = Directory.GetFiles(htmlpath, "*.html");
            foreach (var htmlfile in htmlfiles)
            {
                htmlidx++;
                var rpath = MakeRelativePath(htmlfile, roothtmlpath);
                var encodedrpath = HttpUtility.HtmlEncode(rpath);
                string filename = encodedrpath.Substring(encodedrpath.LastIndexOf("/") + 1);
                AppendWithEncode(builder, "<a href='{0}' target='_self'>[{1}] </a>", encodedrpath, htmlidx);
            }

            var dirs = Directory.GetDirectories(htmlpath);
            if (dirs.Count() > 0)
            {
                AppendWithEncode(builder, "<ul>");
                foreach (var dir in dirs)
                {
                    TraverseIndex(dir, roothtmlpath, builder);
                }
                AppendWithEncode(builder, "</ul>");
            }

            AppendWithEncode(builder, "</li>");
        }

        internal static string PathMode { set; get; }
        static string MakeRelativePath(string path, string rootpath)
        {
            //return path;

            if (PathMode == "absolute")
                return path.Replace('\\', '/');
            else
            {
                if (Directory.Exists(path) && !path.EndsWith("\\"))
                    path += '\\';
                if (Directory.Exists(rootpath) && !rootpath.EndsWith("\\"))
                    rootpath += '\\';

                if (path[0] != rootpath[0])
                    return path.Replace('\\', '/');
                else
                {
                    var pathuri = new Uri("file:///" + path);
                    var rootpathuri = new Uri("file:///" + rootpath);
                    var rp = rootpathuri.MakeRelativeUri(pathuri);
                    return rp.ToString();
                }
            }
        }

        static string[] imgexts = new string[] { ".jpg", ".jpeg", ".bmp", ".gif", ".png" };
        static bool IsImage(string filepath)
        {
            var ext = filepath.Substring(filepath.LastIndexOf('.')).ToLower();
            return imgexts.Contains(ext);
        }

        static string[] videoexts = new string[] { ".mov", ".asf", ".wmv", ".wma", ".mp3", ".mkv", ".rm", ".rmvb", ".avi", ".mp4" };
        static bool IsVideo(string filepath)
        {
            var ext = filepath.Substring(filepath.LastIndexOf('.')).ToLower();
            return videoexts.Contains(ext);
        }

        static string urlescapes = @"<>#%?&$";
        static string RemoveEscapeChar(string ori)
        {
            foreach (var c in urlescapes)
            {
                ori = ori.Replace(c+"", "_");
            }
            return ori;
        }

        static IEnumerable<string> GenerateHtmlForFolder(string path, string htmlpath, int pagesize = 50)
        {
            var foldername = path.Substring(path.LastIndexOf('\\'));

            htmlpath = RemoveEscapeChar(htmlpath);

            var rp = MakeRelativePath(path, htmlpath);

            var files = from f in Directory.EnumerateFiles(path)
                        where IsImage(f) || IsVideo(f)
                        select f;
            if (files.Count() == 0)
                return null;

            if (!Directory.Exists(htmlpath))
                Directory.CreateDirectory(htmlpath);

            // cover
            var imgpath=GetCover(files);
            if (imgpath != null)
            {
                var ext = imgpath.Substring(imgpath.LastIndexOf('.'));
                File.Copy(imgpath, htmlpath + "\\cover" + ext, true);
            }

            // html
            var refiles = new List<string>();
            foreach (var file in files)
            {
                refiles.Add(MakeRelativePath(file, htmlpath));
            }

            var htmlnum = Math.Ceiling(refiles.Count / (double)pagesize);
            var htmlfiles = new List<string>();
            for (int i = 0; i < htmlnum; i++)
            {
                int startpos = i * pagesize;
                int size = Math.Min(pagesize, refiles.Count - startpos);
                var htmldoc = GenerateHtml(foldername, refiles.GetRange(startpos, size));

                var newhtmlpath = htmlpath + "\\" + i.ToString("00000") + ".html";
                SaveToFile(htmldoc, newhtmlpath);
                htmlfiles.Add(newhtmlpath);
            }

            return htmlfiles;
        }

        static string GetCover(IEnumerable<string> files)
        {
            var cover = 
                files.FirstOrDefault(f => GetFileName(f).StartsWith("cover."));
            if (cover == null)
                cover = files.FirstOrDefault(f => IsImage(f));
            return cover;
        }

        static string GetFileName(string filepath)
        {
            var fileinfo = new FileInfo(filepath);
            return fileinfo.Name;
        }

        static XDocument GenerateHtml(string title, IEnumerable<string> files)
        {
            var folderpath=files.First().Substring(0,files.First().LastIndexOf('/'));
            var imgdiv = new XElement("div");
            var htmldoc = new XDocument(
                    new XElement("html",
                        new XElement("header",
                            new XElement("title", title),
                            new XElement("meta",
                                new XAttribute("charset", "UTF-8"))),
                        new XElement("body",
                            new XElement("p", new XElement("h1", title)),
                            imgdiv,
                            new XElement("p"),
                            new XElement("a",
                                new XAttribute("href", folderpath),
                                "打开此文件夹")
                )));

            foreach (var file in files.Where(f => IsVideo(f)))
            {
                char sepchar = '/';
                string filename = file.Substring(file.LastIndexOf(sepchar) + 1);
                string fileext = filename.Substring(filename.LastIndexOf('.')).ToLower();
                string filefolder = file.Substring(0, file.LastIndexOf(sepchar));

                XElement vele = new XElement("div");
                if (fileext == ".mp4" || fileext == ".mkv")
                {
                    vele.Add(
                        new XElement("video",
                            new XAttribute("style", "max-width: 100%; max-height: 100%;"),
                            new XAttribute("src", file),
                            new XAttribute("controls", "controls"),
                            "您的浏览器不支持视频标记"),
                        new XElement("br"));
                }
                vele.Add(new XElement("a",
                    new XAttribute("href", file),
                    filename));
                vele.Add(new XElement("a",
                    new XAttribute("href", filefolder),
                    "[folder]"));
                imgdiv.Add(vele);
            }

            foreach (var file in files.Where(f => IsImage(f)))
            {
                char sepchar = '/';
                string filename = file.Substring(file.LastIndexOf(sepchar) + 1);

                var img = new XElement("div",
                    new XAttribute("style", "margin:10px"),
                    new XElement("img",
                        new XAttribute("style", "max-width: 100%"),
                        new XAttribute("alt", filename),
                        new XAttribute("src", file))
                    );
                imgdiv.Add(img);
            }

            return htmldoc;
        }

        static void SaveToFile(XDocument html, string filepath)
        {
            var str = html.ToString();
            using (var filestream = File.Create(filepath))
            {
                using (var writer = new StreamWriter(filestream))
                {
                    writer.Write(str);
                }
            }
        }

    }
}
