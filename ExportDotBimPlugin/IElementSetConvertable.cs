using export_DOTBIM.Interfaces;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Mesh = Rhino.Geometry.Mesh;

namespace export_DOTBIM.Interfaces
{
    public interface IElementSetConvertable
    {
        BimElementSet ToElementSet();
    }
}

namespace export_DOTBIM
{
    public class BimElementSet : IElementSetConvertable
    {
        public BimElementSet(Mesh mesh, List<Plane> insertPlanes, List<string> guids,
            List<string> types, List<System.Drawing.Color> colors, List<Dictionary<string, string>> infos)
        {
            Mesh = mesh;
            InsertPlanes = insertPlanes;
            Guids = guids;
            Types = types;
            Colors = colors;
            Infos = infos;
            PreviewMeshes = CreatePreviewMeshes();
        }

        public BimElementSet(Mesh mesh, List<Plane> insertPlanes, string type, System.Drawing.Color color,
            Dictionary<string, string> info)
        {
            Mesh = mesh;
            InsertPlanes = insertPlanes;
            Guids = insertPlanes.Select(unused => Guid.NewGuid().ToString()).ToList();
            Types = insertPlanes.Select(unused => type).ToList();
            Colors = insertPlanes.Select(unused => color).ToList();
            Infos = insertPlanes.Select(unused => info).ToList();
            PreviewMeshes = CreatePreviewMeshes();
        }

        public BimElementSet ToElementSet()
        {
            return this;
        }

        private List<Mesh> CreatePreviewMeshes()
        {
            List<Mesh> previewMeshes = new List<Mesh>();

            for (int i = 0; i < InsertPlanes.Count; i++)
            {
                Mesh previewMesh = Mesh.DuplicateMesh();
                previewMesh.Transform(Transform.PlaneToPlane(Plane.WorldXY, InsertPlanes[i]));
                previewMesh.VertexColors.CreateMonotoneMesh(Colors[i]);
                previewMeshes.Add(previewMesh);
            }

            return previewMeshes;
        }

        public Mesh Mesh { get; }
        public List<Plane> InsertPlanes { get; }
        public List<string> Guids { get; }
        public List<string> Types { get; }
        public List<System.Drawing.Color> Colors { get; }
        public List<Dictionary<string, string>> Infos { get; }
        public List<Mesh> PreviewMeshes { get; }
    }

    public class BimElement : IElementSetConvertable
    {
        public BimElement(Mesh mesh, string type, Color color, Dictionary<string, string> info)
        {
            Mesh = mesh;
            Guid = System.Guid.NewGuid().ToString();
            Type = type;
            Color = color;
            Info = info;
            PreviewMesh = CreatePreviewMesh();
        }

        public BimElement(Mesh mesh, string guid, string type, Color color, Dictionary<string, string> info)
        {
            Mesh = mesh;
            Guid = guid;
            Type = type;
            Color = color;
            Info = info;
            PreviewMesh = CreatePreviewMesh();
        }

        public BimElementSet ToElementSet()
        {
            return new BimElementSet(Mesh, new List<Plane> { Plane.WorldXY }, new List<string> { Guid },
                new List<string> { Type }, new List<Color> { Color }, new List<Dictionary<string, string>> { Info });
        }

        private Mesh CreatePreviewMesh()
        {
            Mesh previewMesh = Mesh.DuplicateMesh();
            previewMesh.VertexColors.CreateMonotoneMesh(Color);
            return previewMesh;
        }

        public Mesh Mesh { get; }
        public string Guid { get; }
        public string Type { get; }
        public Color Color { get; }
        public Dictionary<string, string> Info { get; }
        public Mesh PreviewMesh { get; }
    }
}