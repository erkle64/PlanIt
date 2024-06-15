using PlanIt;
using System.Collections.Generic;
using System.IO;
using TinyJSON;

public struct PlanData
{
    public List<string> inputs;
    public List<string> outputs;
    public List<double> outputAmounts;
    public int conveyorTier;
    public int metallurgyTier;
    public int salesTier;
    public bool allowUnresearched;
    public int blastFurnaceTowers;
    public int stoveTowers;
    public int airVentVents;

    public static PlanData Create()
    {
        return new PlanData
        {
            inputs = new List<string>(),
            outputs = new List<string>(),
            outputAmounts = new List<double>(),
            conveyorTier = 0,
            metallurgyTier = 0,
            salesTier = 0,
            allowUnresearched = false,
            blastFurnaceTowers = 1,
            stoveTowers = 1,
            airVentVents = 2
        };
    }

    public static PlanData Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        JSON.MakeInto(JSON.Load(json), out PlanData planData);

        if (planData.inputs == null) planData.inputs = new List<string>();
        if (planData.outputs == null) planData.outputs = new List<string>();
        if (planData.outputAmounts == null) planData.outputAmounts = new List<double>();

        if (planData.outputAmounts.Count > planData.outputs.Count)
        {
            planData.outputAmounts.RemoveRange(planData.outputs.Count, planData.outputAmounts.Count - planData.outputs.Count);
        }
        else while (planData.outputAmounts.Count < planData.outputs.Count)
        {
            planData.outputAmounts.Add(0L);
        }

        return planData;
    }

    public void Save(string filePath)
    {
        var json = JSON.Dump(this);
        File.WriteAllText(filePath, json);
    }
}
