using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using VSLangProj;
using IExtenderProvider = EnvDTE.IExtenderProvider;

namespace ThomasLevesque.AutoRunCustomTool
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidAutoRunCustomToolPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class AutoRunCustomToolPackage : Package
    {
        public AutoRunCustomToolPackage()
        {
            Debug.WriteLine("Entering constructor for: {0}", this);
        }

        #region Package Members

        private DTE _dte;
        private Events _events;
        private DocumentEvents _documentEvents;
        private readonly Dictionary<int, IExtenderProvider> _registerExtenderProviders = new Dictionary<int, IExtenderProvider>();

        public const string TargetsPropertyName = "RunCustomToolOn";

        protected override void Initialize()
        {
            Debug.WriteLine ("Entering Initialize() of: {0}", this);
            base.Initialize();

            _dte = (DTE)GetService(typeof(DTE));
            _events = _dte.Events;
            _documentEvents = _events.DocumentEvents;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;

            RegisterExtenderProvider();

        }

        void RegisterExtenderProvider()
        {
            var provider = new PropertyExtenderProvider(_dte, this);
            string name = PropertyExtenderProvider.ExtenderName;
            RegisterExtenderProvider(VSConstants.CATID.CSharpFileProperties_string, name, provider);
            RegisterExtenderProvider(VSConstants.CATID.VBFileProperties_string, name, provider);
        }

        void RegisterExtenderProvider(string extenderCatId, string name, IExtenderProvider extenderProvider)
        {
            int cookie = _dte.ObjectExtenders.RegisterExtenderProvider(extenderCatId, name, extenderProvider);
            _registerExtenderProviders.Add(cookie, extenderProvider);
        }

        void DocumentEvents_DocumentSaved(Document doc)
        {
            var docItem = doc.ProjectItem;
            if (docItem == null)
                return;

            string docFullPath = (string) GetPropertyValue(docItem, "FullPath");

            var targets = new List<string>();

            string customTool = GetPropertyValue(docItem, "CustomTool") as string;
            if (customTool == "AutoRunCustomTool")
            {
                string targetName = GetPropertyValue(docItem, "CustomToolNamespace") as string;
                if (targetName == null)
                    return;
                targets.Add(targetName);
            }
            else
            {
                var projectName = docItem.ContainingProject.UniqueName;
                IVsSolution solution = (IVsSolution) GetGlobalService(typeof(SVsSolution));
                IVsHierarchy hierarchy;
                solution.GetProjectOfUniqueName(projectName, out hierarchy);

                IVsBuildPropertyStorage storage = hierarchy as IVsBuildPropertyStorage;
                if (storage == null)
                    return;

                uint itemId;
                if (hierarchy.ParseCanonicalName(docFullPath, out itemId) != 0)
                    return;

                string runCustomToolOn;
                if (storage.GetItemAttribute(itemId, TargetsPropertyName, out runCustomToolOn) != 0)
                    return;

                if (runCustomToolOn == null)
                    return;
                targets.AddRange(runCustomToolOn.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            foreach (var targetName in targets)
            {
                string dir = Path.GetDirectoryName(docFullPath);
                // ReSharper disable once AssignNullToNotNullAttribute
                string targetPath = Path.GetFullPath(Path.Combine(dir, targetName));
                var targetItem = _dte.Solution.FindProjectItem(targetPath);
                if (targetItem == null)
                    continue;

                var vsTargetItem = (VSProjectItem)targetItem.Object;
                vsTargetItem.RunCustomTool();
            }
        }

        private static object GetPropertyValue(ProjectItem item, object index)
        {
            try
            {
                var prop = item.Properties.Item(index);
                if (prop != null)
                    return prop.Value;
            }
            catch (ArgumentException) { }
            return null;
        }

        #endregion

    }
}
