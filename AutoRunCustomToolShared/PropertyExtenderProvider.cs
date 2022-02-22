using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace ThomasLevesque.AutoRunCustomTool
{
    [ComVisible(true)]
    [Guid(ExtenderGuid)]
    public class PropertyExtenderProvider : IExtenderProvider
    {
        public const string ExtenderName = "AutoRunCustomTool.PropertyExtenderProvider";
        public const string ExtenderGuid = "124D1A83-20C0-4783-AD6B-032929BEC4B0";

        private readonly DTE _dte;
        private readonly IServiceProvider _serviceProvider;

        public PropertyExtenderProvider(DTE dte, IServiceProvider serviceProvider)
        {
            _dte = dte;
            _serviceProvider = serviceProvider;
        }

        public object GetExtender(string extenderCATID, string extenderName, object extendeeObject, IExtenderSite extenderSite, int cookie)
        {
            dynamic extendee = extendeeObject;
            string fullPath = extendee.FullPath;
            var projectItem = _dte.Solution.FindProjectItem(fullPath);
            IVsSolution solution = (IVsSolution) _serviceProvider.GetService(typeof(SVsSolution));
            IVsHierarchy projectHierarchy;
            if (solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out projectHierarchy) != 0)
                return null;
            uint itemId;
            if (projectHierarchy.ParseCanonicalName(fullPath, out itemId) != 0)
                return null;

            return new PropertyExtender((IVsBuildPropertyStorage) projectHierarchy, itemId, extenderSite, cookie);
        }

        public bool CanExtend(string extenderCATID, string extenderName, object extendeeObject)
        {
            return true;
        }
    }
}