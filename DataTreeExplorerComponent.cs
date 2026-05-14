using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace DataTreeExplorer
{
    public class DataTreeExplorerComponent : GH_Component, IGH_VariableParameterComponent
    {
        public List<bool> BranchStates = new List<bool>();

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
            
            // FIX: If no data is connected, or if data is removed, update the UI instead of exiting blindly
            if (!DA.GetDataTree(0, out inputTree) || inputTree.PathCount == 0)
            {
                BranchStates.Clear();
                if (Params.Output.Count > 0)
                {
                    this.OnPingDocument().ScheduleSolution(5, (doc) => {
                        this.VariableParameterMaintenance();
                        this.ExpireSolution(false);
                    });
                }
                return;
            }

            // Sync states up or down to match the new incoming path count
            if (BranchStates.Count > inputTree.PathCount)
            {
                BranchStates.RemoveRange(inputTree.PathCount, BranchStates.Count - inputTree.PathCount);
            }
            while (BranchStates.Count < inputTree.PathCount) 
            {
                BranchStates.Add(false);
            }

            // If parameter count does not match the tree structure, trigger a re-layout pass
            if (inputTree.PathCount != Params.Output.Count)
            {
                this.OnPingDocument().ScheduleSolution(5, (doc) => {
                    this.VariableParameterMaintenance();
                    this.ExpireSolution(false);
                });
                return;
            }

            // Assign data safely to the ports
            for (int i = 0; i < inputTree.PathCount; i++)
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
            // Dynamically build or reduce ports to perfectly match BranchStates
            while (Params.Output.Count < BranchStates.Count)
            {
                Params.RegisterOutputParam(CreateParameter(GH_ParameterSide.Output, Params.Output.Count));
            }
            while (Params.Output.Count > BranchStates.Count)
            {
                Params.UnregisterParameter(Params.Output[Params.Output.Count - 1]);
            }

            for (int i = 0; i < Params.Output.Count; i++)
            {
                Params.Output[i].NickName = "B" + i;
                Params.Output[i].Name = "Branch " + i;
                Params.Output[i].Access = GH_ParamAccess.list;
                if (Params.Output[i].Attributes == null) Params.Output[i].CreateAttributes();
            }
            
            Params.OnParametersChanged();
        }

        public override Guid ComponentGuid => new Guid("4f5a29b1-4343-4035-989e-044e8580d9cf");
    }
}