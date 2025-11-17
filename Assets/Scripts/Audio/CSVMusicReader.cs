using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class CSVMusicReader
{
    public static List<MusicTrackData> ReadMusicCSV(string csvFileName)
    {
        List<MusicTrackData> trackList = new List<MusicTrackData>();

        TextAsset csvFile = Resources.Load<TextAsset>(csvFileName);
        if (csvFile == null)
        {
            Debug.LogError($"CSV file '{csvFileName}' not found in Resources folder!");
            return trackList;
        }

        string[] lines = csvFile.text.Split('\n');

        // Skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = SplitCSVLine(line);
            if (values.Length >= 5)
            {
                try
                {
                    string songName = values[0].Trim();
                    float loopStart = float.Parse(values[1].Trim());
                    float loopEnd = float.Parse(values[2].Trim());
                    bool isBattleTrack = values[3].Trim().ToLower() == "true";
                    bool isMenuTrack = values[4].Trim().ToLower() == "true";

                    trackList.Add(new MusicTrackData(songName, loopStart, loopEnd, isBattleTrack, isMenuTrack));
                } catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing line {i}: {line}\n{e.Message}");
                }
            }
        }

        return trackList;
    }

    private static string[] SplitCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            } else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            } else
            {
                currentField += c;
            }
        }

        result.Add(currentField);
        return result.ToArray();
    }
}