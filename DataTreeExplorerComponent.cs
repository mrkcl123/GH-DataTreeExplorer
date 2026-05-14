using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
public override void CreateAttributes()
{
    m_attributes = new DataTreeExplorerAttributes(this);
}
public class DataTreeExplorerComponent : GH_Component, IGH_VariableParameterComponent
{
    // Store the 'Open/Closed' state of branches
    public List<bool> BranchStates = new List<bool>();

    // ... (Constructor and Guid go here) ...

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_Structure<IGH_Goo> inputTree = new GH_Structure<IGH_Goo>();
        if (!DA.GetDataTree(0, out inputTree)) return;

        // Ensure BranchStates list matches the tree size
        while (BranchStates.Count < inputTree.PathCount) BranchStates.Add(false);

        // Sync outputs if needed
        if (inputTree.PathCount != Params.Output.Count)
        {
            this.OnPingDocument().ScheduleSolution(5, (doc) => {
                this.ExpireSolution(false);
            });
            return;
        }

        for (int i = 0; i < inputTree.PathCount; i++)
        {
            DA.SetDataList(i, inputTree.get_Branch(i));
        }
    }

    // --- IGH_VariableParameterComponent Implementation ---
    public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
    public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
    public IGH_Param CreateParameter(GH_ParameterSide side, int index) => new Grasshopper.Kernel.Parameters.Param_GenericObject();
    public bool DestroyParameter(GH_ParameterSide side, int index) => true;
    public void VariableParameterMaintenance() 
    { 
        // Logic to name ports based on branch index
        for (int i = 0; i < Params.Output.Count; i++)
        {
            Params.Output[i].NickName = "B" + i;
            Params.Output[i].Name = "Branch " + i;
        }
    }
}