using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProceduralToolkit.Buildings
{
    public class BuildingGenerator
    {
        private IFacadePlanner facadePlanner;
        private IFacadeConstructor facadeConstructor;
        private IRoofPlanner roofPlanner;
        private IRoofConstructor roofConstructor;

        public void SetFacadePlanner(IFacadePlanner facadePlanner)
        {
            this.facadePlanner = facadePlanner;
        }

        public void SetFacadeConstructor(IFacadeConstructor facadeConstructor)
        {
            this.facadeConstructor = facadeConstructor;
        }

        public void SetRoofPlanner(IRoofPlanner roofPlanner)
        {
            this.roofPlanner = roofPlanner;
        }

        public void SetRoofConstructor(IRoofConstructor roofConstructor)
        {
            this.roofConstructor = roofConstructor;
        }

        public Transform Generate(List<Vector2> foundationPolygon, Config config, Transform parent = null)
        {
            Assert.IsTrue(config.floors > 0);
            Assert.IsTrue(config.entranceInterval > 0);

            List<ILayout> facadeLayouts = facadePlanner.Plan(foundationPolygon, config);
            float height = facadeLayouts[0].height;

            if (parent == null)
            {
                parent = new GameObject("Building").transform;
            }
            facadeConstructor.Construct(foundationPolygon, facadeLayouts, parent);

            if (roofPlanner != null && roofConstructor != null)
            {
                var roofConstructible = roofPlanner.Plan(foundationPolygon, config);

                var roof = new GameObject("Roof").transform;
                roof.SetParent(parent, false);
                roof.localPosition = new Vector3(0, height, 0);
                roof.localRotation = Quaternion.identity;
                roofConstructor.Construct(roofConstructible, roof);
            }
            return parent;
        }

        [Serializable]
        public class Config
        {
            public int floors = 5;
            public float entranceInterval = 12;
            public bool hasAttic = true;
            public RoofConfig roofConfig = new RoofConfig
            {
                type = RoofType.Flat,
                thickness = 0.2f,
                overhang = 0.2f,
            };
            public Palette palette = new Palette();
        }
    }

    [Serializable]
    public class RoofConfig
    {
        public RoofType type = RoofType.Flat;
        public float thickness;
        public float overhang;
    }

    [Serializable]
    public class Palette
    {
        public Color socleColor = ColorE.silver;
        public Color socleWindowColor = (ColorE.silver/2).WithA(1);
        public Color doorColor = (ColorE.silver/2).WithA(1);
        public Color wallColor = ColorE.white;
        public Color frameColor = ColorE.silver;
        public Color glassColor = ColorE.white;
        public Color roofColor = (ColorE.gray/4).WithA(1);
    }

    public enum RoofType
    {
        Flat,
        Hipped,
        Gabled,
    }
}
