using System.Linq;
using NUnit.Framework;
using pxr;
using Unity.Formats.USD;
using UnityEditor;
using UnityEngine;
using USD.NET.Tests;

namespace USD.NET.Unity.Tests
{
    class InstancingTests : UsdTests
    {
        Scene CreatePointInstancerScene()
        {
            var tmpScenePath = CreateTmpUsdFile("PointInstancerTest.usda");
            var scene = Scene.Open(tmpScenePath);
            var pi = new PointInstancerSample();
            var cube = new CubeSample();

            pi.prototypes.targetPaths = new[] { "/Instancer/Cube" };

            // Three instances, all prototype index zero.
            pi.protoIndices = new int[3];
            pi.positions = new Vector3[3];
            pi.positions[0] = new Vector3(0, 0, 0);
            pi.positions[1] = new Vector3(2, 0, 0);
            pi.positions[2] = new Vector3(4, 0, 0);

            scene.Write("/Instancer", pi);
            scene.Write("/Instancer/Cube", cube);

            scene.Save();
            return scene;
        }

        Scene CreateMeshInstanceScene()
        {
            var cube = new CubeSample();
            var xform = new XformSample();

            var cubeScenePath = CreateTmpUsdFile("cube.usda");
            var scene = Scene.Open(cubeScenePath);
            scene.Write("/GEO", xform);
            scene.Write("/GEO/Cube", cube);
            scene.Save();
            scene.Close();

            var tmpScenePath = CreateTmpUsdFile("InstanceTraversalTest.usda");
            scene = Scene.Open(tmpScenePath);
            scene.Write("/InstancedCube1", xform);
            scene.Write("/InstancedCube1/cube", cube);
            scene.Write("/InstancedCube2", xform);
            scene.Write("/InstancedCube2/cube", cube);

            var prim = scene.GetPrimAtPath("/InstancedCube1/cube");
            prim.GetReferences().AddReference(cubeScenePath, new SdfPath("/GEO/Cube"));
            prim.SetInstanceable(true);
            prim = scene.GetPrimAtPath("/InstancedCube2/cube");
            prim.GetReferences().AddReference(cubeScenePath, new SdfPath("/GEO/Cube"));
            prim.SetInstanceable(true);
            scene.Save();
            return scene;
        }

        [Test]
        public void TraversalTest()
        {
            var scene = CreateMeshInstanceScene();
            foreach (UsdPrim prim in scene.Stage.Traverse(new Usd_PrimFlagsPredicate(Usd_PrimFlags.Usd_PrimInstanceFlag)))
            {
                Assert.IsTrue(prim.IsInstance(), "Prim {0} is not an instance.", prim.GetPath());
                var master = prim.GetMaster();
                Assert.AreEqual("/GEO/Cube", master.GetPath());
                Assert.AreEqual(2, master.GetAllDescendants().Count());
            }

            scene.Close();
        }

        [Test]
        public void PointInstancerTest()
        {
            var scene = CreatePointInstancerScene();
            var piSample = new PointInstancerSample();
            scene.Read("/Instancer", piSample);

            Assert.AreEqual(3, piSample.protoIndices.Length);
            Assert.AreEqual(1, piSample.prototypes.targetPaths.Length);
            Assert.AreEqual("/Instancer/Cube", piSample.prototypes.targetPaths[0]);

            var matrices = piSample.ComputeInstanceMatrices(scene, "/Instancer");
            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(Matrix4x4.Translate(new Vector3(0 + i * 2, 0, 0)), matrices[i]);
            }
            scene.Close();
        }

        [TestCase("UsdInstance_UpAxisY_LeftHanded")]
        [TestCase("UsdInstance_UpAxisY_RightHanded")]
        [TestCase("UsdInstance_UpAxisZ_LeftHanded")]
        [TestCase("UsdInstance_UpAxisZ_RightHanded")]
        public void InstancerImport_VertexCheck(string testFileName)
        {
            var originalVertices = new[]
            {
                new Vector3(-1, 0, 3),
                new Vector3(-1, 0, 0),
                new Vector3(0, -1, 1),
                new Vector3(-2, -3, -1),
                new Vector3(-2, -1, 1),
                new Vector3(-3, -0.5f, -1),
                new Vector3(-1, -2, 1),
                new Vector3(-5, -2, -1)
            };
            var testScene = ImportHelpers.InitForOpen(GetTestAssetPath(testFileName));

            var testInstanceObjectMesh = ImportHelpers.ImportSceneAsGameObject(testScene).GetComponentInChildren<MeshFilter>().mesh;

            foreach (var vertex in testInstanceObjectMesh.vertices)
            {
                // Flip the z-axis value to compensate for the z-axis difference between Unity and USD
                var zFlippedVertex = new Vector3(vertex.x, vertex.y, -vertex.z);
                Assert.Contains(zFlippedVertex, originalVertices, $"Z Axis flipped vertex <{zFlippedVertex}> not found in original vertices");
            }
        }
    }
}
