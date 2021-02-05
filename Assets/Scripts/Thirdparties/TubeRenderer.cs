// Author: Mathias Soeholm
// Date: 05/10/2016
// No license, do whatever you want with this script
using UnityEngine;
using UnityEngine.Serialization;

namespace Thirdparties
{
	[ExecuteInEditMode]
	public class TubeRenderer : MonoBehaviour
	{
		[SerializeField] Vector3[] _positions;
		[SerializeField] int       _sides;
		[SerializeField] float     _radiusOne;

		private Vector3[]    _vertices;
		private Mesh         _mesh;
		private MeshFilter   _meshFilter;
		private MeshRenderer _meshRenderer;
		private float        _oldSides  = -1;
		private float        _oldRadiusOne = 0;

		public Material material
		{
			get { return _meshRenderer.material; }
			set { _meshRenderer.material = value; }
		}

		void Awake()
		{
			_meshFilter = GetComponent<MeshFilter>();
			if (_meshFilter == null)
			{
				_meshFilter = gameObject.AddComponent<MeshFilter>();
			}

			_meshRenderer = GetComponent<MeshRenderer>();
			if (_meshRenderer == null)
			{
				_meshRenderer = gameObject.AddComponent<MeshRenderer>();
			}

			_mesh = new Mesh();
			_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			_meshFilter.mesh = _mesh;
		}

		private void OnEnable()
		{
			_meshRenderer.enabled = true;
		}

		private void OnDisable()
		{
			_meshRenderer.enabled = false;
		}

		void Update()
		{
			if(_oldRadiusOne != _radiusOne || _oldSides != _sides)
				GenerateMesh();
			_oldRadiusOne = _radiusOne;
			_oldSides     = _sides;
		}

		private void OnValidate()
		{
			_sides = Mathf.Max(3, _sides);
		}

		public void SetPositions(Vector3[] positions)
		{
			_positions = positions;
			GenerateMesh();
		}

		private void GenerateMesh()
		{
			if (_mesh == null || _positions == null || _positions.Length <= 1)
			{
				_mesh = new Mesh();
				_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				return;
			}

			//Regenerate Indices
			var verticesLength = _sides * _positions.Length;
			if (_vertices == null || _vertices.Length != verticesLength)
			{
				_vertices = new Vector3[verticesLength];
				var indices = GenerateIndices();

				//Meshes are weird regarding arrays' sizes
				if (verticesLength > _mesh.vertexCount)
				{
					_mesh.vertices = _vertices;
					_mesh.triangles = indices;
				}
				else
				{
					_mesh.triangles = indices;
					_mesh.vertices = _vertices;
				}
			}

			var currentVertIndex = 0;
			for (int i = 0; i < _positions.Length; i++)
			{
				var circle = CalculateCircle(i);
				foreach (var vertex in circle)
					_vertices[currentVertIndex++] = vertex;
			}

			_mesh.SetVertices(_vertices);
			_mesh.RecalculateNormals();
			_mesh.RecalculateBounds();

			_meshFilter.mesh = _mesh;
		}

		private int[] GenerateIndices()
		{
			// Two triangles and 3 vertices
			var indices = new int[_positions.Length * _sides * 2 * 3];

			var currentIndicesIndex = 0;
			for (int segment = 1; segment < _positions.Length; segment++)
			{
				for (int side = 0; side < _sides; side++)
				{
					var vertIndex = (segment * _sides + side);
					var prevVertIndex = vertIndex - _sides;

					// Triangle one
					indices[currentIndicesIndex++] = prevVertIndex;
					indices[currentIndicesIndex++] = (side == _sides - 1) ? (vertIndex - (_sides - 1)) : (vertIndex + 1);
					indices[currentIndicesIndex++] = vertIndex;


					// Triangle two
					indices[currentIndicesIndex++] = (side == _sides - 1) ? (prevVertIndex - (_sides - 1)) : (prevVertIndex + 1);
					indices[currentIndicesIndex++] = (side == _sides - 1) ? (vertIndex - (_sides - 1)) : (vertIndex + 1);
					indices[currentIndicesIndex++] = prevVertIndex;
				}
			}

			return indices;
		}

		private Vector3[] CalculateCircle(int index)
		{
			var dirCount = 0;
			var forward = Vector3.zero;

			// If not first index
			if (index > 0)
			{
				forward += (_positions[index] - _positions[index - 1]).normalized;
				dirCount++;
			}

			// If not last index
			if (index < _positions.Length - 1)
			{
				forward += (_positions[index + 1] - _positions[index]).normalized;
				dirCount++;
			}

			// Forward is the average of the connecting edges directions
			forward   = (forward / dirCount).normalized;
			var side  = Vector3.Cross(forward, forward + new Vector3(.123564f, .34675f, .756892f)).normalized;
			var up    = Vector3.Cross(forward, side).normalized;

			var circle = new Vector3[_sides];
			var angle = 0f;
			var angleStep = (2 * Mathf.PI) / _sides;

			var radius = _radiusOne;

			for (int i = 0; i < _sides; i++)
			{
				var x = Mathf.Cos(angle);
				var y = Mathf.Sin(angle);

				circle[i] = _positions[index] + radius*(side * x  + up * y);
				angle += angleStep;
			}

			return circle;
		}
	}
}