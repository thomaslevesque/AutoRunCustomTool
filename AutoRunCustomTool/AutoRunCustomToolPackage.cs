using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace ThomasLevesque.AutoRunCustomTool
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidAutoRunCustomToolPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class AutoRunCustomToolPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public AutoRunCustomToolPackage()
        {
            Debug.WriteLine("Entering constructor for: {0}", this);
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        private DTE _dte;
        private Events _events;
        private DocumentEvents _documentEvents;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine ("Entering Initialize() of: {0}", this);
            base.Initialize();

            _dte = (DTE)GetService(typeof(DTE));
            _events = _dte.Events;
            _documentEvents = _events.DocumentEvents;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
        }

        void DocumentEvents_DocumentSaved(Document doc)
        {
            var docItem = doc.ProjectItem;
            if (docItem == null)
                return;

            string customTool = GetPropertyValue(docItem, "CustomTool") as string;
            if (customTool != "AutoRunCustomTool")
                return;

            string targetName = GetPropertyValue(docItem, "CustomToolNamespace") as string;
            if (targetName == null)
                return;

            string dir = Path.GetDirectoryName(docItem.FileNames[0]);
            string targetPath = Path.Combine(dir, targetName);
            var targetItem = docItem.DTE.Solution.FindProjectItem(targetPath);
            if (targetItem == null)
                return;

            var vsTargetItem = (VSProjectItem) targetItem.Object;
            vsTargetItem.RunCustomTool();

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
