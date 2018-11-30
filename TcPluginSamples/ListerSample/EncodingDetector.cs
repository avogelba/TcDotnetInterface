// This code was downloaded from MOBZystems, Home of Tools (www.mobzystems.com). 
// No license, use at will. And at your own risk!

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OY.TotalCommander.TcPlugins.ListerSample    
{
  /// <summary>
  /// EncodingDetector. Detects the Encoding used in byte arrays
  /// or files by testing the start of the file for a Byte Order Mark
  /// (called 'preamble' in .NET).
  /// 
  /// Use ReadAllText() to read a file using a detected encoding.
  /// 
  /// All encodings that have a preamble are supported.
  /// </summary>
  public class EncodingDetector
  {
    /// <summary>
    /// Helper class to store information about encodings
    /// with a preamble
    /// </summary>
    protected class PreambleInfo
    {
      /// <summary>
      /// Property Encoding (Encoding).
      /// </summary>
      public Encoding Encoding { get; private set; }
 
      /// <summary>
      /// Property Preamble (byte[]).
      /// </summary>
      public byte[] Preamble { get; private set; }
 
      /// <summary>
      /// Constructor with preamble and encoding
      /// </summary>
      /// <param name="encoding"></param>
      /// <param name="preamble"></param>
      public PreambleInfo(Encoding encoding, byte[] preamble)
      {
        Encoding = encoding;
        Preamble = preamble;
      }
    }
 
    // The list of encodings with a preamble,
    // sorted longest preamble first.
    protected static SortedList<int, PreambleInfo> preambles = null;
 
    // Maximum length of all preamles
    protected static int maxPreambleLength = 0;
 
    /// <summary>
    /// Read the contents of a text file as a string. Scan for a preamble first.
    /// If a preamble is found, the corresponding encoding is used.
    /// If no preamble is found, the supplied defaultEncoding is used.
    /// </summary>
    /// <param name="filename">The name of the file to read</param>
    /// <param name="defaultEncoding">The encoding to use if no preamble present</param>
    /// <param name="usedEncoding">The actual encoding used</param>
    /// <returns>The contents of the file as a string</returns>
    public static string ReadAllText(string filename, Encoding defaultEncoding, out Encoding usedEncoding)
    {
      // Read the contents of the file as an array of bytes
      byte[] bytes = File.ReadAllBytes(filename);
 
      // Detect the encoding of the file:
      usedEncoding = DetectEncoding(bytes);
 
      // If none found, use the default encoding.
      // Otherwise, determine the length of the encoding markers in the file
      int offset;
      if (usedEncoding == null)
      {
        offset = 0;
        usedEncoding = defaultEncoding;
      }
      else
      {
        offset = usedEncoding.GetPreamble().Length;
      }
 
      // Now interpret the bytes according to the encoding,
      // skipping the preample (if any)
      return usedEncoding.GetString(bytes, offset, bytes.Length - offset);
    }
 
    /// <summary>
    /// Detect the encoding in an array of bytes.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>The encoding found, or null</returns>
    public static Encoding DetectEncoding(byte[] bytes)
    {
      // Scan for encodings if we haven't done so
      if (preambles == null)
        ScanEncodings();
 
      // Try each preamble in turn
      foreach (PreambleInfo info in preambles.Values)
      {
        // Match all bytes in the preamble
        bool match = true;
 
        if (bytes.Length >= info.Preamble.Length)
        {
          for (int i = 0; i < info.Preamble.Length; i++)
          {
            if (bytes[i] != info.Preamble[i])
            {
              match = false;
              break;
            }
          }
          if (match)
          {
            return info.Encoding;
          }
        }
      }
 
      return null;
    }
 
    /// <summary>
    /// Detect the encoding of a file. Reads just enough of
    /// the file to be able to detect a preamble.
    /// </summary>
    /// <param name="filename">The path name of the file</param>
    /// <returns>The encoding detected, or null if no preamble found</returns>
    public static Encoding DetectEncoding(string filename)
    {
      // Scan for encodings if we haven't done so
      if (preambles == null)
        ScanEncodings();
 
      using (FileStream stream = File.OpenRead(filename))
      {
        // Never read more than the length of the file
        // or the maximum preamble length
        long n = stream.Length;
 
        // No bytes? No encoding!
        if (n == 0)
          return null;
 
        // Read the minimum amount necessary
        if (n > maxPreambleLength)
          n = maxPreambleLength;
 
        byte[] bytes = new byte[n];
 
        stream.Read(bytes, 0, (int)n);
 
        // Detect the encoding from the byte array
        return DetectEncoding(bytes);
      }
    }
 
    /// <summary>
    /// Loop over all available encodings and store those
    /// with a preamble in the preambles list.
    /// The list is sorted by preamble length,
    /// longest preamble first. This prevents
    /// a short preamble 'masking' a longer one
    /// later in the list.
    /// </summary>
    protected static void ScanEncodings()
    {
      // Create a new sorted list of preambles
      preambles = new SortedList<int, PreambleInfo>();
 
      // Loop over all encodings
      foreach (EncodingInfo encodingInfo in Encoding.GetEncodings())
      {
        // Do we have a preamble?
        byte[] preamble = encodingInfo.GetEncoding().GetPreamble();
        if (preamble.Length > 0)
        {
          // Add it to the collection, inversely sorted by preamble length
          // (and code page, to keep the keys unique)
          preambles.Add(-(preamble.Length * 1000000 + encodingInfo.CodePage),
             new PreambleInfo(encodingInfo.GetEncoding(), preamble));
 
          // Update the maximum preamble length if this one's longer
          if (preamble.Length > maxPreambleLength)
          {
            maxPreambleLength = preamble.Length;
          }
        }
      }
    }

    // Added by Oleg Yuvashev
    // Read the contents of a byte array as a string. Works like ReadAllText.
    public static string GetString(byte[] bytes, Encoding defaultEncoding)
    {
        // Detect the encoding of the byte array:
        Encoding  usedEncoding = DetectEncoding(bytes);

        // If none found, use the default encoding.
        // Otherwise, determine the length of the encoding markers in the file
        int offset;
        if (usedEncoding == null)
        {
            offset = 0;
            usedEncoding = defaultEncoding;
        }
        else
        {
            offset = usedEncoding.GetPreamble().Length;
        }

        // Now interpret the bytes according to the encoding,
        // skipping the preample (if any)
        return usedEncoding.GetString(bytes, offset, bytes.Length - offset);
    }

    public static string GetString(byte[] bytes)
    {
        return GetString(bytes, Encoding.Default);
    }
  }
}