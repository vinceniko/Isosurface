# Isosurface

## Next steps

* Place mesh vertex (corners of the quads) in each cube of the cartesian grid
  * Choose vertex location using weighted mass (average)
  * Then QEF function
    * Requires normals
      * calc normals from function

```py
def normal_from_function(f, d=0.01):
    """Given a sufficiently smooth 3d function, f, returns a function approximating of the gradient of f.
    d controls the scale, smaller values are a more accurate approximation."""
    def norm(x, y, z):
        return V3(
            (f(x + d, y, z) - f(x - d, y, z)) / 2 / d,
            (f(x, y + d, z) - f(x, y - d, z)) / 2 / d,
            (f(x, y, z + d) - f(x, y, z - d)) / 2 / d,
        ).normalize()
    return norm
```

```cpp
float3 EstimateNormal(float3 p) {
    float x = SceneInfo(float3(p.x+epsilon,p.y,p.z)).w - SceneInfo(float3(p.x-epsilon,p.y,p.z)).w;
    float y = SceneInfo(float3(p.x,p.y+epsilon,p.z)).w - SceneInfo(float3(p.x,p.y-epsilon,p.z)).w;
    float z = SceneInfo(float3(p.x,p.y,p.z+epsilon)).w - SceneInfo(float3(p.x,p.y,p.z-epsilon)).w;
    return normalize(float3(x,y,z));
}
```

* connect mesh surface vertices with edges to form quads

## Place mesh vertex (corners of the quads) in each cube of the cartesian grid

* Know which cube edges intersect with the surface
  * Iterate over each cube
  * evaluate the function at each edge end point
  * check for sign differences
  * add them to an array to linearly interpolate edge positions

```c++
// inputs
uniform edge_length;
signed_distance_field[];


struct Edge {
  Vector3 start;
  Vector3 end;
};

edge_length = size_grid / resolution;
for (top_front_left_vertex: grid) {
  Edge edges[]; 

  // displacement along dimensions
  Vector3 x = {1, 0, 0};
  Vector3 y = {0, 1, 0};
  Vector3 z = {0, 0, 1};
  Vector3 dimensions[] = {x, y, z};
  for (i_dim, dim: dimensions) { // we want edges along this dimension
    Vector3 other_dimensions[] = dimensions.filter((dimension) {dimensions !== dim});

    Vector3 edges_starting_points[]
    for (other_dim : other_dimensions) {
      edges_starting_points.push(top_front_left_vertex + other_dim * edge_length);
    }

    for (starting_point: edges_starting_points) {
      edges.push(Edge{
        starting_point,
        starting_point + dim * edge_length,
      })
    }
  }


  for (Edge edge : cube_edges) {
    if (eval(edge.start) * eval(edge.end) > 0) { // need signed distance field, index into it
      edges_on_surface.push(edge)
    }
  }

  Vector3 edge_crossings[];
  for (Edge edge : edges_on_surface) {
    edge_crossing = linearly_interpolate_vertex(edge); // need signed distance field
    edge_crossings[].push(edge_crossing);
  } 

  Vector3 sum = {0}
  count = 0;
  for (vertex: edge_crossings) {
    sum += vertex;
    count++;
  }
  surface_pos = sum / count;

  return surface_pos;
}
```

Debugging / Presentation:

Instantiate sprite at the position of each surface point. Should end up in between points in the grid.

* Interpolate the vertex position on each edge

![alt](notes/edge-interpolation.jpeg)

* Weighted mass of the vertex positions on the edges