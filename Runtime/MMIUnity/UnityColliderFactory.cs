// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer

using MMIStandard;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace MMIUnity
{
    /// <summary>
    /// Class instantiates Unity colliders based on the MCollider class 
    /// </summary>
    public static class UnityColliderFactory
    {
        /// <summary>
        /// Creates a Unity collider based on the MCollider and a transform
        /// </summary>
        /// <param name="collider"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static Collider CreateCollider(MCollider collider, MTransform transform)
        {
            if (collider == null || transform == null)
                return null;

			Vector3 scale = getScale(collider);

			Collider result = null;
            switch (collider.Type)
            {
                case MColliderType.Box:
					result = getBoxCollider(collider);
					break;
					
                case MColliderType.Sphere:
					result = getSphereCollider(collider, scale);
					break;

                case MColliderType.Capsule:
					result = getCapsuleCollider(collider);
					break;

				case MColliderType.Cylinder:
					result = getCylinderCollider(collider);
					break;

				case MColliderType.Mesh:
					result = getMeshCollider(collider);
					break;

				default:
					Debug.Log($"Collider type \"{collider.Type}\" is not implemented yet");
					return null;
			}

			if (result != null)
			{
				var gameobject = result.gameObject;
				gameobject.transform.position = transform.Position.ToVector3();
				gameobject.transform.rotation = transform.Rotation.ToQuaternion();
				gameobject.transform.localScale = scale;
			}

			return result;
		}

		private static BoxCollider getBoxCollider(MCollider mCollider)
		{
			MBoxColliderProperties boxProps = mCollider.BoxColliderProperties;
			if (boxProps == null)
			{
				Debug.Log("Box collider is null");
				return null;
			}

			GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
			BoxCollider boxCollider = box.GetComponent<BoxCollider>();
			boxCollider.center = mCollider.PositionOffset.ToVector3();
			boxCollider.size = boxProps.Size.ToVector3();

			return boxCollider;
		}

		private static SphereCollider getSphereCollider(MCollider mCollider, Vector3 scale)
		{
			MSphereColliderProperties sphereProps = mCollider.SphereColliderProperties;
			if (sphereProps == null)
			{
				Debug.Log("Sphere collider is null");
				return null;
			}

			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			SphereCollider sphereCollider = sphere.GetComponent<SphereCollider>();
			sphereCollider.center = mCollider.PositionOffset.ToVector3();
			sphereCollider.radius = (float)sphereProps.Radius / Mathf.Max(scale.x, scale.y, scale.z);

			return sphereCollider;
		}

		private static CapsuleCollider getCapsuleCollider(MCollider mCollider)
		{
			MCapsuleColliderProperties capsuleProps = mCollider.CapsuleColliderProperties;
			if (capsuleProps == null)
			{
				Debug.Log("Capsule collider is null");
				return null;
			}

			GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			CapsuleCollider capsuleCollider = capsule.GetComponent<CapsuleCollider>();
			capsuleCollider.center = mCollider.PositionOffset.ToVector3();
			capsuleCollider.radius = (float)capsuleProps.Radius;
			capsuleCollider.height = (float)capsuleProps.Height;
			
			return capsuleCollider;
		}

		private static CapsuleCollider getCylinderCollider(MCollider mCollider)
		{
			MCylinderColliderProperties cylinderProps = mCollider.CylinderColliderProperties;

			if (cylinderProps == null)
			{
				Debug.Log("Cylinder collider is null");
				return null;
			}

			GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			CapsuleCollider cylinderCollider = cylinder.GetComponent<CapsuleCollider>();
			cylinderCollider.center = mCollider.PositionOffset.ToVector3();
			cylinderCollider.radius = (float)cylinderProps.Radius;
			cylinderCollider.height = (float)cylinderProps.Height;

			return cylinderCollider;
		}

		private static MeshCollider getMeshCollider(MCollider mCollider)
		{
			MMeshColliderProperties mMeshCollider = mCollider.MeshColliderProperties;

			if (mMeshCollider == null)
			{
				Debug.Log("Mesh collider is null");
				return null;
			}

			GameObject mesh = new GameObject();
			MeshFilter meshFilter = mesh.AddComponent<MeshFilter>();
			MeshRenderer renderer = mesh.AddComponent<MeshRenderer>();

			meshFilter.mesh.SetVertices(mMeshCollider.Vertices.Select(s => s.ToVector3()).ToList());
			meshFilter.mesh.SetTriangles(mMeshCollider.Triangles, 0);
			meshFilter.mesh.RecalculateNormals();

			MeshCollider meshCollider = mesh.AddComponent<MeshCollider>();
			meshCollider.sharedMesh = meshFilter.mesh;

			return meshCollider;
		}

		//ToDo: remove method and replace with transform.Scale.ToVector3() once scale info is added to MTransform
		private static Vector3 getScale(MCollider mCollider)
		{
			Vector3 result = Vector3.one;
			if(mCollider.Properties != null && mCollider.Properties.Count > 0)
			{
				if (mCollider.Properties.TryGetValue("ScaleX", out string scaleXStr) &&
					float.TryParse(scaleXStr, NumberStyles.Any,
						CultureInfo.InvariantCulture, out float scaleX))
					result.x = scaleX;
				
				if (mCollider.Properties.TryGetValue("ScaleY", out string scaleYStr) &&
					float.TryParse(scaleYStr, NumberStyles.Any,
						CultureInfo.InvariantCulture, out float scaleY))
					result.y = scaleY;
				
				if (mCollider.Properties.TryGetValue("ScaleZ", out string scaleZStr) &&
					float.TryParse(scaleZStr, NumberStyles.Any,
						CultureInfo.InvariantCulture, out float scaleZ))
					result.z = scaleZ;
			}
			return result;
		}
    }
}
