using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Gif.Components;
using NAudio.Lame;
using NAudio.Wave;
using Ultima;

namespace ServUO_Services.Controllers
{
    public class ArtController : Controller
    {
        [Route]
        public ActionResult Index()
        {
            return View();
        }

        [Route("API")]
        public ActionResult Api()
        {
            return View();
        }

        [Route("uoapi/item/{id:int}/{hue?}")]
        public ActionResult ItemArt(int id, int hue = 0)
        {
            DeleteTempFiles(System.Web.HttpContext.Current);
            Files.SetMulPath(HttpContext.Server.MapPath("~/mul"));
            Bitmap bmp = new Bitmap(Art.GetStatic(id));
            if (hue > 0 && hue <= 3000)
            {
                short[] colors = Hues.GetHue(hue).Colors;
                Hues.ApplyTo(bmp, colors, (TileData.ItemTable[id].Flags & TileFlag.PartialHue) != 0);
            }

            byte[] byteArray = ImageToByte(bmp);

            using (Image image = Image.FromStream(new MemoryStream(byteArray)))
            {
                image.Save(HttpContext.Server.MapPath("~/tempfiles/") + id + hue + ".png", ImageFormat.Png);
            }

            ViewBag.img = "/tempfiles/" + id + hue + ".png";
            return File(HttpContext.Server.MapPath("~/tempfiles/") + id + hue + ".png", "image/png");

        }

        [Route("uoapi/gump/{id:int}/{hue?}")]
        public ActionResult GumpArt(int id, int hue = 0)
        {
            DeleteTempFiles(System.Web.HttpContext.Current);
            Files.SetMulPath(HttpContext.Server.MapPath("~/mul"));
            Bitmap bmp;

            if (hue > 0 && hue <= 3000)
            {
                bool patched;
                bmp = new Bitmap(Gumps.GetGump(id, Hues.GetHue(hue), false, out patched));
            }
            else
            {
                bmp = new Bitmap(Gumps.GetGump(id));
            }

            byte[] byteArray = ImageToByte(bmp);

            using (Image image = Image.FromStream(new MemoryStream(byteArray)))
            {
                image.Save(HttpContext.Server.MapPath("~/tempfiles/") + id + hue + ".png", ImageFormat.Png);
            }

            ViewBag.img = "/tempfiles/" + id + hue + ".png";
            return File(HttpContext.Server.MapPath("~/tempfiles/") + id + hue + ".png", "image/png");

        }

        [Route("uoapi/multi/{id:int}/{hue?}")]
        public ActionResult MultiArt(int id, int hue = 0)
        {
            DeleteTempFiles(System.Web.HttpContext.Current);
            Files.SetMulPath(HttpContext.Server.MapPath("~/mul"));
            Bitmap bmp = new Bitmap(Multis.GetComponents(id).GetImage());
            if (hue > 0 && hue <= 3000)
            {
                short[] colors = Hues.GetHue(hue).Colors;
                Hues.ApplyTo(bmp, colors, (TileData.ItemTable[id].Flags & TileFlag.PartialHue) != 0);
            }
            byte[] byteArray = ImageToByte(bmp);
            using (Image image = Image.FromStream(new MemoryStream(byteArray)))
            {
                image.Save(HttpContext.Server.MapPath("~/tempfiles/") + id + hue + ".png", ImageFormat.Png);
            }

            ViewBag.img = "/tempfiles/" + id + hue + ".png";
            return File(HttpContext.Server.MapPath("~/tempfiles/") + id + hue + ".png", "image/png");

        }

        [Route("uoapi/sound/{id:int}")]
        public ActionResult Sound(int id)
        {
            DeleteTempFiles(System.Web.HttpContext.Current);
            Files.SetMulPath(HttpContext.Server.MapPath("~/mul"));
            var sound = Sounds.GetSound(id);
            CheckAddBinPath();
            var mp3 = ConvertWavToMp3(sound.buffer);
            using (MemoryStream m = new MemoryStream(mp3))
            {
                using (FileStream fs = new FileStream(HttpContext.Server.MapPath("~/tempfiles/") + id + ".mp3", FileMode.Create))
                {
                    m.WriteTo(fs);
                }
            }

            return File(HttpContext.Server.MapPath("~/tempfiles/") + id + ".mp3", "audio/mpeg");
        }

        [Route("uoapi/anim/{id:int}/{hue?}")]
        public ActionResult Anim(int id, int hue = 0)
        {
            DeleteTempFiles(System.Web.HttpContext.Current);
            Files.SetMulPath(HttpContext.Server.MapPath("~/mul"));
            Animations.Translate(ref id);
            Frame[] anim = Animations.GetAnimation(id, 0, 1, ref hue, false, false);
            List<Bitmap> bmps = anim.Select(f => f.Bitmap).ToList();

            return File(BuildAnimation(bmps, 0, System.Web.HttpContext.Current), "image/gif");
        }

        [Route("uoapi/anim/{id:int}/{act:int}/{direction:int}/{firstframe?}/{hue?}/{repeat?}")]
        public ActionResult Anim(int id, int act, int direction, bool firstframe = false, int hue = 0, int repeat = 0)
        {
            DeleteTempFiles(System.Web.HttpContext.Current);
            Files.SetMulPath(HttpContext.Server.MapPath("~/mul"));
            Animations.Translate(ref id);
            Frame[] anim = Animations.GetAnimation(id, act, direction, ref hue, false, firstframe);
            List<Bitmap> bmps = anim.Select(f => f.Bitmap).ToList();
            return File(BuildAnimation(bmps, repeat, System.Web.HttpContext.Current), "image/gif");
        }

        [Route("uoapi/cliloc/{id:int}/{language?}")]
        public ActionResult Cliloc(int id, string language = "enu")
        {
            DeleteTempFiles(System.Web.HttpContext.Current);
            Files.SetMulPath(HttpContext.Server.MapPath("~/mul"));
            StringList sl = new StringList(language);
            sl.GetEntry(id);

            return Content(sl.GetEntry(id).Text);
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        public static byte[] ConvertWavToMp3(byte[] wavFile)
        {

            using (var retMs = new MemoryStream())
            using (var ms = new MemoryStream(wavFile))
            using (var rdr = new WaveFileReader(ms))
            using (var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, 128))
            {
                rdr.CopyTo(wtr);
                return retMs.ToArray();
            }
        }

        public static string BuildAnimation(List<Bitmap> bmps, int repeat, HttpContext http)
        {
            string guid = Guid.NewGuid().ToString();
            string final = http.Server.MapPath("~/tempfiles/") + "anim" + guid + ".gif";
            AnimatedGifEncoder e = new AnimatedGifEncoder();
            
            using (MemoryStream m = new MemoryStream())
            {
                using (FileStream fs = new FileStream(final, FileMode.Create))
                {
                    
                    e.Start(m);
                    e.SetTransparent(Color.Black);
                    e.SetDelay(10);
                    int width = 0;
                    int height = 0;
                    foreach (var b in bmps)
                    {
                        if (b.Width > width)
                            width = b.Width;
                        if (b.Height > height)
                            height = b.Height;

                    }
                    e.SetRepeat(repeat);
                    e.SetSize(width, height);
                    foreach (var b in bmps)
                    {

                        e.AddFrame(b);
                        b.Dispose();
                    }

                    e.Finish();
                    m.WriteTo(fs);
                }
            }
            return final;

        }

        public static void DeleteTempFiles(HttpContext http)
        {
            DirectoryInfo di = new DirectoryInfo(http.Server.MapPath("~/tempfiles/"));
            foreach (FileInfo file in di.GetFiles())
            {
                if (file.CreationTimeUtc + TimeSpan.FromMinutes(5) < DateTime.UtcNow )
                    file.Delete();
            }

        }
         
        public static void CheckAddBinPath()
        {

            var binPath = Path.Combine(new[] { AppDomain.CurrentDomain.BaseDirectory, "bin" });
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!path.Split(Path.PathSeparator).Contains(binPath, StringComparer.CurrentCultureIgnoreCase))
            {
                path = string.Join(Path.PathSeparator.ToString(), path, binPath);
                Environment.SetEnvironmentVariable("PATH", path);
            }
        }

    }
}
