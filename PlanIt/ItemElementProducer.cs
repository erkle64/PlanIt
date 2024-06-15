using UnityEngine;

namespace PlanIt
{
    internal class ItemElementProducer
    {
        public readonly string identifier;
        public readonly string name;
        public readonly Sprite icon;
        public double speed;
        public readonly double powerUsage;

        public ItemElementProducer(string identifier, string name, Sprite icon, double speed, double powerUsage)
        {
            this.identifier = identifier;
            this.name = name;
            this.icon = icon;
            this.speed = speed;
            this.powerUsage = powerUsage;
        }
    }
}
