using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace ThomasLevesque.AutoRunCustomTool
{
    [ComVisible(true)]
    public class PropertyExtender
    {
        private readonly IVsBuildPropertyStorage _storage;
        private readonly uint _itemId;
        private readonly IExtenderSite _extenderSite;
        private readonly int _cookie;

        public PropertyExtender(IVsBuildPropertyStorage storage, uint itemId, IExtenderSite extenderSite, int cookie)
        {
            _storage = storage;
            _itemId = itemId;
            _extenderSite = extenderSite;
            _cookie = cookie;
        }

        ~PropertyExtender()
        {
            try
            {
                if (_extenderSite != null)
                    _extenderSite.NotifyDelete(_cookie);
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Error in PropertyExtender finalizer: {0}", ex);
            }
        }

        [Category("AutoRunCustomTool")]
        [DisplayName("Run custom tool on")]
        [Description("When this file is saved, the custom tool will be run on the files listed in this field")]
        [Editor("System.Windows.Forms.Design.StringArrayEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        public string[] RunCustomToolOn
        {
            get
            {
                return LoadRunCustomToolOn();
            }
            set
            {
                SaveRunCustomToolOn(value);
            }
        }

        private string[] LoadRunCustomToolOn()
        {
            string s;
            _storage.GetItemAttribute(_itemId, AutoRunCustomToolPackage.TargetsPropertyName, out s);
            if (s != null)
            {
                return s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }
            return null;
        }

        private void SaveRunCustomToolOn(string[] items)
        {
            string s = null;
            if (items != null)
            {
                s = string.Join(";", items);
            }
            _storage.SetItemAttribute(_itemId, AutoRunCustomToolPackage.TargetsPropertyName, s);
        }
    }
}