using System;
using UnityEngine;

namespace JalanKita
{
    // Explicit prototype scoring rubric, NOT a legal classification of offence severity.
    public static class PracticeRating
    {
        public static bool IsSerious(Rule rule) => rule != Rule.ReturnLeft
            && rule != Rule.PickupPlace && rule != Rule.PickupIncomplete;
        public static int Total(int[] counts)
        { int n = 0; if (counts != null) foreach (int x in counts) n += Mathf.Max(0, x); return n; }
        public static int Serious(int[] counts)
        { int n = 0; if (counts != null) for (int i=0;i<counts.Length && i<Lesson.All.Length;i++)
            if (IsSerious((Rule)i)) n += Mathf.Max(0, counts[i]); return n; }
        public static int Stars(bool complete, int[] counts) => !complete ? 0 : Total(counts) == 0 ? 3 : Serious(counts) == 0 ? 2 : 1;
        public static string Symbols(int stars) { stars = Mathf.Clamp(stars,0,3); return new string('★',stars)+new string('☆',3-stars); }
        public static int[] Since(int[] counts, int[] baseline)
        {
            var result = new int[Lesson.All.Length];
            for (int i=0;i<result.Length;i++) result[i] = Mathf.Max(0,(counts != null && i<counts.Length?counts[i]:0)-(baseline != null && i<baseline.Length?baseline[i]:0));
            return result;
        }
    }

    [Serializable]
    public sealed class PracticeScore
    {
        public int bestStars, lastStars, completions, lastMistakes, lastSerious, missionCompletionSeen;
        public static string Key(VehicleMode mode,string id) => "RoadSense.PracticeRating.v1."+mode+"."+id;
        public static PracticeScore Load(VehicleMode mode,string id)
        {
            try { var value=JsonUtility.FromJson<PracticeScore>(PlayerPrefs.GetString(Key(mode,id),""));
                if(value!=null && value.bestStars>=0 && value.bestStars<=3) return value; }
            catch(ArgumentException) { }
            return new PracticeScore();
        }
        public static PracticeScore RecordMission(VehicleMode mode,Mission mission,JourneySave save)
        {
            string id="mission-"+mission.Id; var value=Load(mode,id);
            if(save.completions <= value.missionCompletionSeen) return value;
            value=Record(mode,id,save.mistakes); value.missionCompletionSeen=save.completions;
            PlayerPrefs.SetString(Key(mode,id),JsonUtility.ToJson(value)); PlayerPrefs.Save(); return value;
        }
        public static PracticeScore Record(VehicleMode mode,string id,int[] mistakes)
        {
            var value=Load(mode,id); value.lastStars=PracticeRating.Stars(true,mistakes);
            value.bestStars=Mathf.Max(value.bestStars,value.lastStars); value.completions++;
            value.lastMistakes=PracticeRating.Total(mistakes); value.lastSerious=PracticeRating.Serious(mistakes);
            PlayerPrefs.SetString(Key(mode,id),JsonUtility.ToJson(value)); PlayerPrefs.Save(); return value;
        }
    }
}
