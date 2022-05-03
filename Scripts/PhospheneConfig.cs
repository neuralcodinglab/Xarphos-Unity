using UnityEngine;
using System.IO;
using System.Collections.Generic;


namespace Xarphos.Scripts
{

  public struct Phosphene
  {
    public Vector2 position;
    public float size;
    public float activation;
    public float trace;
  }

  public class PhospheneConfig
  {   // Data class containing the phosphene configuration (count, locations, sizes)
      // instances can be directly deseriallized from a JSON file using the 'load' method

      public string description = "PHOSPHENE SPECIFICATIONS FILE.  'nPhosphenes': the number of phosphenes. 'eccentricities': the radius (in degrees of visual angle) from the foveal center for each phosphene. 'azimuth_angles': the polar angle (in radians) for each phosphene. 'size': each phosphene's default size (in dva). ";
      public int nPhosphenes;
      public float[] eccentricities;
      public float[] azimuth_angles;
      public float[] sizes;

      public static PhospheneConfig load(string filename)
      {
        string json = System.IO.File.ReadAllText(filename);
        return JsonUtility.FromJson<PhospheneConfig>(json);
      }

      public void save(string filename)
      {
        string json = JsonUtility.ToJson(this);
        File.WriteAllText(filename, json);
        Debug.Log("Saved phosphene configuration to " + filename);
      }

      public static Phosphene[] InitPhosphenesFromJSON(string filename, Vector2 FieldOfView)
      {
        // Initializes a struct-array with all phosphenes. Note that this struct
        // array (Phosphene) contains more properties than only position and size
        PhospheneConfig config = PhospheneConfig.load(filename);
        Debug.Log(config.nPhosphenes);
        Debug.Log(config.description);
        Phosphene[] phosphenes = new Phosphene[config.nPhosphenes];
        for (int i=0; i<config.nPhosphenes; i++)
        {
          var x = config.eccentricities[i] * Mathf.Cos(config.azimuth_angles[i]);
          var y = config.eccentricities[i] * Mathf.Sin(config.azimuth_angles[i]);
          phosphenes[i].position = new Vector2(0.5f,0.5f) + new Vector2(x,y) / FieldOfView;
          phosphenes[i].size = config.sizes[i] / FieldOfView.x;
        }
        return phosphenes;
      }

  }
}
