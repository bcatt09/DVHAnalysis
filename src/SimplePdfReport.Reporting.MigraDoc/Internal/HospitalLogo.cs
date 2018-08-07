using System;
using System.Reflection;

namespace SimplePdfReport.Reporting.MigraDoc.Internal
{
    internal static class HospitalLogo
    {
        private const string HospitalResourceName = "Resources.logo.png";

        public static string GetMigraDocFileName()
        {
            return ConvertToMigraDocFileName(LoadResource(HospitalResourceName));
        }

        // Use special feature in MigraDoc, where instead of using a real file name,
        // it uses a special name that reads the image from memory
        // (see http://www.pdfsharp.net/wiki/MigraDoc_FilelessImages.ashx)
        private static string ConvertToMigraDocFileName(byte[] image)
        {
            return $"base64:{Convert.ToBase64String(image)}";
        }

        private static byte[] LoadResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullName = $"{assembly.GetName().Name}.{name}";
            using (var stream = assembly.GetManifestResourceStream(fullName))
            {
                if (stream == null)
                {
                    throw new ArgumentException($"No resource with name {name}");
                }

                var count = (int)stream.Length;
                var data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }
    }
}