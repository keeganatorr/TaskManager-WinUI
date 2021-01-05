using System;
using System.IO;

namespace TaskmanWindow
{
    public class OpenProcess
    {
        public string LocalFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\";
        public string FilePath = "";
        //private string _Icon = "ms-appdata:///local/image.jpg";
        private string _Icon = "/Assets/blank.exe.png";
        public string Icon
        {
            get { return _Icon; } 
            set {
                if (File.Exists($"{LocalFolderPath}{value}.png"))
                {
                    _Icon = $"ms-appdata:///local/{value}.png";
                }
            }
        }
    }
}
