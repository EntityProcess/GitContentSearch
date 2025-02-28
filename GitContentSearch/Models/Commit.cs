namespace GitContentSearch.Models
{
    public class Commit
    {
        public string Hash { get; set; }
        public string Time { get; set; }

        public Commit(string hash, string time)
        {
            Hash = hash;
            Time = time;
        }
    }
} 