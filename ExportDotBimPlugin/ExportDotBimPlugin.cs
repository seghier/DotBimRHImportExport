using dotbim;
using export_DOTBIM.Interfaces;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Render.ChangeQueue;
using System;
using System.Collections;
using System.Collections.Generic;

namespace export_DOTBIM
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class ExportDotBimPlugin : Rhino.PlugIns.FileExportPlugIn
    {
        public ExportDotBimPlugin()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the ExportDotBimPlugin plug-in.</summary>
        public static ExportDotBimPlugin Instance { get; private set; }

        /// <summary>Defines file extensions that this export plug-in is designed to write.</summary>
        /// <param name="options">Options that specify how to write files.</param>
        /// <returns>A list of file types that can be exported.</returns>
        protected override Rhino.PlugIns.FileTypeList AddFileTypes(Rhino.FileIO.FileWriteOptions options)
        {
            var result = new Rhino.PlugIns.FileTypeList();
            result.AddFileType("DotBim file (*.bim)", "bim");
            return result;
        }

        /// <summary>
        /// Is called when a user requests to export a .bim file.
        /// It is actually up to this method to write the file itself.
        /// </summary>
        /// <param name="filename">The complete path to the new file.</param>
        /// <param name="index">The index of the file type as it had been specified by the AddFileTypes method.</param>
        /// <param name="doc">The document to be written.</param>
        /// <param name="options">Options that specify how to write file.</param>
        /// <returns>A value that defines success or a specific failure.</returns>
        protected override Rhino.PlugIns.WriteFileResult WriteFile(string filename, int index, RhinoDoc doc, Rhino.FileIO.FileWriteOptions options)
        {
            try
            {

                int mehLevel = 0;
                var gs = new GetNumber();
                gs.SetCommandPrompt("Meshing Level 0 or 1");
                gs.AcceptNothing(true);
                gs.Get();
                if (gs.CommandResult() == Result.Success)
                    mehLevel = (int)gs.Number();

                List<IElementSetConvertable> elementSetConvertables = new List<IElementSetConvertable>();
                List<dotbim.Mesh> dotmeshes = new List<dotbim.Mesh>();
                List<Element> dotelements = new List<Element>();
                int currentMeshId = 0;

                foreach (var geo in doc.Objects.GetSelectedObjects(false, false))
                {
                    if (geo == null) continue;

                    //var inputmesh = (Rhino.Geometry.Mesh)geo.Geometry;
                    Rhino.Geometry.Mesh inputmesh = new Rhino.Geometry.Mesh();
                    inputmesh = RhinoObjToMesh(geo, mehLevel);

                    var mesh = inputmesh.DuplicateMesh();
                    mesh.Faces.ConvertQuadsToTriangles();

                    var dotmesh = Tools.CreateBimMeshFromRhinoMesh(mesh, currentMeshId);
                    dotmeshes.Add(dotmesh);

                    var plane = Rhino.Geometry.Plane.WorldXY;
                    string id = geo.Id.ToString();
                    string type = "";
                    var color = geo.Attributes.ObjectColor;
                    Dictionary<string, string> info = new Dictionary<string, string>();

                    for (int i = 0; i < geo.Attributes.UserStringCount; i++)
                    {
                        string key = geo.Attributes.GetUserStrings().Keys[i];
                        string value = geo.Attributes.GetUserStrings().Get(key);

                        if (key == "Type")
                            type = value;

                        info.Add(key, value); // Use Add instead of reassigning the entire dictionary
                    }

                    if (mesh.GetType() == typeof(Rhino.Geometry.Mesh))
                    {
                        //BimElementSet element = new BimElementSet(mesh, planes, guids, types, colors, infos);
                        BimElement element = new BimElement(mesh, id, type, color, info);

                        BimElementSet bimElementSet = element.ToElementSet();
                        for (int i = 0; i < bimElementSet.InsertPlanes.Count; i++)
                        {

                            Element elemA = new Element
                            {
                                MeshId = currentMeshId,
                                Vector = Tools.ConvertInsertPlaneToVector(bimElementSet.InsertPlanes[i]),
                                Rotation = Tools.ConvertInsertPlaneToRotation(bimElementSet.InsertPlanes[i]),
                                Guid = bimElementSet.Guids[i],
                                Info = bimElementSet.Infos[i],
                                Type = bimElementSet.Types[i]
                            };
                            Tools.SetFacesColor(elemA, mesh);

                            Element elemB = new Element
                            {
                                MeshId = currentMeshId,
                                Vector = Tools.ConvertInsertPlaneToVector(bimElementSet.InsertPlanes[i]),
                                Rotation = Tools.ConvertInsertPlaneToRotation(bimElementSet.InsertPlanes[i]),
                                Color = Tools.GetBimColorFromDrawingColor(bimElementSet.Colors[i]),
                                Guid = bimElementSet.Guids[i],
                                Info = bimElementSet.Infos[i],
                                Type = bimElementSet.Types[i]
                            };

                            if (Tools.chooseColor == true)
                                dotelements.Add(elemA);
                            else
                                dotelements.Add(elemB);
                        }

                        currentMeshId += 1;

                        elementSetConvertables.Add(element);
                    }
                }

                Dictionary<string, string> fileInfo = new Dictionary<string, string>() { { "Rhino " + RhinoApp.ExeVersion.ToString(), doc.Name } };
                //File file = Tools.CreateFile(elementSetConvertables, fileInfo);
                File file = Tools.CreateFile(dotelements, dotmeshes, fileInfo);
                file.Save(filename);
            }
            catch (Exception ex)
            {
                string error = Rhino.PlugIns.WriteFileResult.Failure.ToString();
                RhinoApp.WriteLine($"Error exporting BIM file {error}: {ex.Message}");
                return Rhino.PlugIns.WriteFileResult.Failure; // Return Failure result in case of an exception
            }

            return Rhino.PlugIns.WriteFileResult.Success;
        }


        Rhino.Geometry.Mesh RhinoObjToMesh (Rhino.DocObjects.RhinoObject obj, int level)
        {
            var mesh = new Rhino.Geometry.Mesh();
            if (obj.Geometry.GetType() == typeof(SubD))
            {
                mesh = Rhino.Geometry.Mesh.CreateFromSubD((SubD)obj.Geometry,0);
                //mesh = Rhino.Geometry.Mesh.CreateFromSubDControlNet((SubD)obj.Geometry);
            }
            else if (obj.Geometry.GetType() == typeof(Rhino.Geometry.Mesh))
            {
                mesh = (Rhino.Geometry.Mesh)obj.Geometry;
            }
            else
            {
                obj.CreateMeshes(MeshType.Preview, meshingPars[level], true);
                foreach (var m in obj.GetMeshes(MeshType.Preview))
                    mesh.Append(m);
            }
            return mesh;
        }

        List<MeshingParameters> meshingPars = new List<MeshingParameters>
                    {
                        MeshingParameters.Minimal,
                        MeshingParameters.Default
                    };

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.
    }
}
