using PlanIt;
using System.Collections.Generic;
using System.IO;
using TinyJSON;

public struct PlanData
{
    public List<string> inputs;
    public List<string> outputs;
    public List<float> outputAmounts;
    public int conveyorTier;
    public int assemblerTier;
    public int crusherTier;
    public int metallurgyTier;
    public bool useAdvancedSmelter;
    public bool allowUnresearched;

    public static PlanData Create()
    {
        return new PlanData
        {
            inputs = new List<string>(),
            outputs = new List<string>(),
            outputAmounts = new List<float>(),
            conveyorTier = 0,
            assemblerTier = 0,
            crusherTier = 0,
            metallurgyTier = 0,
            useAdvancedSmelter = false,
            allowUnresearched = false
        };
    }

    public static PlanData Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        JSON.MakeInto(JSON.Load(json), out PlanData planData);

        if (planData.inputs == null) planData.inputs = new List<string>();
        if (planData.outputs == null) planData.outputs = new List<string>();
        if (planData.outputAmounts == null) planData.outputAmounts = new List<float>();

        if (planData.outputAmounts.Count > planData.outputs.Count)
        {
            planData.outputAmounts.RemoveRange(planData.outputs.Count, planData.outputAmounts.Count - planData.outputs.Count);
        }
        else while (planData.outputAmounts.Count < planData.outputs.Count)
        {
            planData.outputAmounts.Add(0.0f);
        }

        return planData;
    }

    public void Save(string filePath)
    {
        var json = JSON.Dump(this);
        File.WriteAllText(filePath, json);
    }
}
