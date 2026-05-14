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

        // Constructor: Sets the name and category in Grasshopper
        public DataTreeExplorerComponent()
          : base("Data Tree Explorer", "TreeExp", "Explores Data Trees like a File Browser", "Sets", "Tree")
        {
        }

        // Required: Link the custom UI attributes
        public override void CreateAttributes()
        {
            m_attributes = new DataTreeExplorerAttributes(this);
        }

        // Required: Define Input
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Tree", "T", "The data tree to explore", GH_ParamAccess.tree);
        }

        // Required: Define Output (Starts empty, will be dynamic)
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

protected override void SolveInstance(IGH_DataAccess DA)
{
    GH_Structure<IGH_Goo> inputTree = new GH_Structure<IGH_Goo>();
    if (!DA.GetDataTree(0, out inputTree)) return;

    // 1. Sync the states list to match the number of branches
    while (BranchStates.Count < inputTree.PathCount) BranchStates.Add(false);

    // 2. CHECK: If the branch count changed, we MUST refresh the UI
    if (inputTree.PathCount != Params.Output.Count)
    {
        // This tells Grasshopper to run VariableParameterMaintenance
        this.OnPingDocument().ScheduleSolution(5, (doc) => {
            // ExpireSolution(true) recomputes the whole component including UI
            this.ExpireSolution(false); 
        });
        return;
    }

    // 3. Output the data
    for (int i = 0; i < inputTree.PathCount; i++)
    {
        DA.SetDataList(i, inputTree.get_Branch(i));
    }
}

        // --- Variable Parameter Implementation ---
        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index) => new Grasshopper.Kernel.Parameters.Param_GenericObject();
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;
        public void VariableParameterMaintenance()
        {
            for (int i = 0; i < Params.Output.Count; i++)
            {
                Params.Output[i].NickName = "B" + i;
                Params.Output[i].Name = "Branch " + i;
            }
        }

        // Required: Unique ID (Generated for you)
        public override Guid ComponentGuid => new Guid("4f5a29b1-4343-4035-989e-044e8580d9cf");
    }
}