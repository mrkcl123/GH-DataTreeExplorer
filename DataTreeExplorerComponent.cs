using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace DataTreeExplorer
{
    public class DataTreeExplorerComponent : GH_Component, IGH_VariableParameterComponent
    {
        // Tracks expanded folder strings (e.g., "Root", "{0}", "{0;1}")
        public HashSet<string> ExpandedFolders = new HashSet<string> { "Root" };
        public List<GH_Path> CurrentPaths = new List<GH_Path>();
        public GH_Structure<IGH_Goo> CurrentTree = new GH_Structure<IGH_Goo>();

        public DataTreeExplorerComponent()
          : base("Data Tree Explorer", "TreeExp", "Explores Data Trees like a File Browser", "Sets", "Tree")
        {
        }

        public override void CreateAttributes()
        {
            m_attributes = new DataTreeExplorerAttributes(this);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Tree", "T", "The data tree to explore", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_Goo> inputTree = new GH_Structure<IGH_Goo>();
            
            if (!DA.GetDataTree(0, out inputTree) || inputTree.PathCount == 0)
            {
                CurrentPaths.Clear();
                CurrentTree.Clear();
                ExpandedFolders.Clear();
                ExpandedFolders.Add("Root");
                
                if (Params.Output.Count > 0)
                {
                    this.OnPingDocument().ScheduleSolution(5, (doc) => {
                        this.VariableParameterMaintenance();
                        this.ExpireSolution(false);
                    });
                }
                return;
            }

            CurrentTree = inputTree.ShallowDuplicate();
            CurrentPaths = new List<GH_Path>(inputTree.Paths);

            this.OnPingDocument().ScheduleSolution(5, (doc) => {
                this.VariableParameterMaintenance();
                this.ExpireSolution(false);
            });

            // Keep physical outputs aligned with the real data branches
            for (int i = 0; i < Math.Min(Params.Output.Count, inputTree.PathCount); i++)
            {
                DA.SetDataList(i, inputTree.get_Branch(i));
            }
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index) => new Grasshopper.Kernel.Parameters.Param_GenericObject();
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;

        public void VariableParameterMaintenance()
        {
            while (Params.Output.Count < CurrentPaths.Count)
            {
                Params.RegisterOutputParam(CreateParameter(GH_ParameterSide.Output, Params.Output.Count));
            }
            while (Params.Output.Count > CurrentPaths.Count)
            {
                Params.UnregisterParameter(Params.Output[Params.Output.Count - 1]);
            }

            for (int i = 0; i < Params.Output.Count; i++)
            {
                string pathString = CurrentPaths[i].ToString();
                Params.Output[i].NickName = pathString;
                Params.Output[i].Name = "Branch " + pathString;
                Params.Output[i].Access = GH_ParamAccess.list;
                if (Params.Output[i].Attributes == null) Params.Output[i].CreateAttributes();
            }
            
            Params.OnParametersChanged();
        }

        public override Guid ComponentGuid => new Guid("4f5a29b1-4343-4035-989e-044e8580d9cf");
    }
}