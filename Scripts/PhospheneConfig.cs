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

      public string description = "PHOSPHENE SPECIFICATIONS FILE.  'nPhosphenes': the number of phosphenes. 'positions': the (x,y) position in screen coordinate (range 0 to 1).  'sizes': the size (sigma) of each phosphene relative to the screen.";
      public int nPhosphenes;
      public Vector2[] positions;
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

      public static Phosphene[] InitPhosphenesFromJSON(string filename)
      {
        // Initializes a struct-array with all phosphenes. Note that this struct
        // array (Phosphene) contains more properties than only position and size
        PhospheneConfig config = PhospheneConfig.load(filename);
        Phosphene[] phosphenes = new Phosphene[config.nPhosphenes];
        for (int i=0; i<config.nPhosphenes; i++)
        {
          phosphenes[i].position = config.positions[i];
          phosphenes[i].size = config.sizes[i];
        }
        return phosphenes;
      }

  }
}
