using UnityEngine;

namespace PlanIt
{
    internal class ItemElementProducer
    {
        public readonly string identifier;
        public readonly string name;
        public readonly Sprite icon;
        public Rational speed;
        public readonly Rational powerUsage;

        public ItemElementProducer(string identifier, string name, Sprite icon, Rational speed, Rational powerUsage)
        {
            this.identifier = identifier;
            this.name = name;
            this.icon = icon;
            this.speed = speed;
            this.powerUsage = powerUsage;
        }
    }
}
