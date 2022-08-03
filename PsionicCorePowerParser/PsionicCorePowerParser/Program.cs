using System.Text.RegularExpressions;

namespace PsionicCorePowerParser
{
    class Program
    {
        private static readonly Regex StaticRegex = new (@"(\d+[d0-9]*)([ \-’a-z]*)", RegexOptions.IgnoreCase);
        private static readonly Regex ScalingRegex = new (@"(\d+|One)([ \-’a-z\.]+)(?:\/|per )levels?(?: of caster)?", RegexOptions.IgnoreCase);
        private static readonly Regex MultiplyRegex = new (@"’ ?x ?", RegexOptions.IgnoreCase);
        private static readonly Regex DashRegex = new (@"\w(-)\w", RegexOptions.IgnoreCase);

        private const string BaseSpellRegex = @"(?<!\*){0}(?!\*)";
        private const string DelimiterRegex = @"(?:,|\.| spell)";

        private static readonly List<Regex> SpellRegex = new List<Regex>()
        {
            // new (string.Format(BaseSpellRegex, "(MM)")),
            // new (string.Format(BaseSpellRegex, "(MC)")),
            // new (string.Format(BaseSpellRegex, "(DMG)")),
            // new (string.Format(BaseSpellRegex, "(PHB)")),
            // new (string.Format(BaseSpellRegex, "(Player’s Option: Combat & Tactics)")),
            // new (string.Format(BaseSpellRegex, "(Tome of Magic)")),
        };

        private const string Book = @"The Complete Psionics Handbook";
        private static string Discipline = "CLAIRSENTIENT";
        private static string PowerLevel = "Science";
        private static string ScalingClass = "";


        public static void Main(string[] args)
        {
            const string page = "28";
            
            var input = @"
Aura Sight
Power Score:Wis -5
Initial Cost:9
Maintenance Cost:9/round
Range:50 yds.
Preparation Time:0
Area of Effect:personal
Prerequisites:none







An aura is a glowing halo or envelope of colored light which surrounds all living things. It is invisible to the naked eye. A creature's aura reflects both its alignment and its experience level.
When a psionicist uses this power, he can see auras. Interpreting an aura requires some concentration, however. With each use of this power, the psionicist can learn only one piece of information-either the subject's alignment or experience level, but not both simultaneously.
A psionicist can examine up to two auras per round (he must be able to see both subjects). Alternately, he can examine the same aura twice, to verify his first impression with a second reading or to pick up remaining information. In any case, the psionicist must make a new power check each time he attempts to interpret an aura.
The psionicist can be reasonably discreet when he uses this power. He doesn't have to poke at the subject or give him the hairy eyeball. However, he does need to gaze at the subject intently. Since the range of this power is the range of vision, the psionicist can go unnoticed by maintaining his distance. If he tries to sense auras on the people he is conversing with, they certainly will notice that he is staring and probably will be uncomfortable.
The level of the character being analyzed affects the psionicist's power check. The higher the subject's experience level, the tougher it is to interpret the subject's aura. This translates into a -1 penalty for every three levels of the subject, rounded down. For example, a psionicist reading the aura of an 8th level character would suffer a -2 penalty.
If the die roll for the power check is a 1, the psionicist's reading is incomplete or slightly incorrect. For example, the psionicist may learn only the chaotic portion of a chaotic neutral alignment. Or he may interpret the character's level with an error of one or two levels.
Power Score - The psionicist can examine up to four auras per round instead of two.
20 - The initiator can't use this power again for 24 hours.
";

            var split = input.Split(new [] {"Table of Contents"}, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var spells = split.Select(s => FormatPower(s, page));
            var output = string.Join("\n", spells);

            var spellFile = Path.Join(SolutionDirectory(), "powers.txt");
            File.WriteAllText(spellFile, output);
        }

        private static string FormatPower(string input, string page)
        {
            input = input
                .Trim()
                .Replace('\'', '’')
                .Replace("*", "&ast;")
                .Replace('·', '•');

            input = MultiplyRegex.Replace(input, "’✕");

            var strings = input
                .Split(new [] {"\n", "\r"}, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var name = strings[0].Trim();
            strings.RemoveAt(0);

            var powerScore = strings.GetAndRemove("^Power Score:");
            var split = powerScore.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var powerScoreAttribute = split[0];
            var powerScoreModifier = split[1];

            var initialCost = strings.GetAndRemove("^Initial Cost:");
            var maintenanceCost = strings.GetAndRemove("^Maintenance Cost:");
            
            var range = strings.GetAndRemove("^Range:");
            range = OverwriteWithScaling(range);
            
            var prepTime = strings.GetAndRemove("^Preparation Time:");
            var aoe = strings.GetAndRemove("^Area of Effect:");
            var preReq = strings.GetAndRemove("^Prerequisites:");
            var powerScoreEffect = strings.GetAndRemove("^Power Score -");
            var effect20 = strings.GetAndRemove("^20 -");

            var effectStrings = strings
                .Select(s =>
                {
                    var dashMatch = DashRegex.Match(s);
                    if (dashMatch.Success)
                    {
                        s = s.Replace('-', '—');
                    }

                    return s;
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Append($"*Power Score*—{powerScoreEffect}")
                .Append($"*20*—{effect20}")
                .ToList();

            var effect = string
                .Join("\\n&emsp;", effectStrings);

            var list = new List<string>();
            list.Add($"{Discipline}['{name}'] = {{");
            list.Add($"'type': '{PowerLevel}',");
            list.Add($"'power-score-attribute': '{powerScoreAttribute}',");
            list.Add($"'power-score-modifier': '{powerScoreModifier}',");
            list.Add($"'initial-cost': '{initialCost}',");
            list.Add($"'maintenance-cost': '{maintenanceCost}',");
            list.Add($"'range': '{range}',");
            list.Add($"'prep-time': '{prepTime}',");
            list.Add($"'aoe': '{aoe}',");
            list.Add($"'prerequisites': '{preReq}',");
            list.Add($"'reference': 'p. {page}',");
            list.Add($"'book': '{Book}',");
            list.Add($"'damage': '',");
            list.Add($"'damage-type': '',");
            list.Add($"'healing': '',");
            list.Add($"'power-score-effect': '{powerScoreEffect}',");
            list.Add($"'20': '{effect20}',");
            list.Add($"'effect': '{effect}'\n}};");
            var output = string.Join("\n    ", list);

            Console.WriteLine($"Done with {name}");
            return output;
        }

        private static string SolutionDirectory()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }

            return directory.FullName;
        }

        private static string OverwriteWithScaling(string s)
        {
            var matchScaling = ScalingRegex.Match(s);
            Match matchStatic;
            if (!matchScaling.Success)
            {
                matchStatic = StaticRegex.Match(s);
                if (matchStatic.Success)
                {
                    var staticPlural = matchStatic.Groups[2].Value.EndsWith("s", StringComparison.OrdinalIgnoreCase) 
                        ? "s"
                        : "";
                    return $"{ParseUnit(s)}{staticPlural}";
                }
            }

            if (!s.Contains('+'))
                return OverwriteWithScalingBase(s, matchScaling);
            
            var staticString = s.Split('+', StringSplitOptions.TrimEntries)[0];
            matchStatic = StaticRegex.Match(staticString);
            if (!matchStatic.Success)
                return OverwriteWithScalingBase(s, matchScaling);
            
            var staticAmount = matchStatic.Groups[1].Value;
            var staticUnit = ParseUnit(matchStatic.Groups[2].Value);
            
            var scalingAmount = matchScaling.Groups[1].Value;
            if (scalingAmount.Equals("One", StringComparison.OrdinalIgnoreCase))
                scalingAmount = "1";
            
            var scalingUnit = ParseUnit(matchScaling.Groups[2].Value);
            
            if (!string.IsNullOrWhiteSpace(staticUnit) && staticUnit != scalingUnit)
                return OverwriteWithScalingBase(s, matchScaling);

            var plural = scalingUnit.EndsWith("feet", StringComparison.OrdinalIgnoreCase)
                ? ""
                : "s";
            
            if (int.TryParse(scalingAmount, out var amount) && amount == 1)
            {
                return $"[[{staticAmount}+{ScalingClass} ]] {scalingUnit}{plural}";
            }

            if (amount > 1)
            {
                return $"[[{staticAmount}+{scalingAmount}*{ScalingClass} ]] {scalingUnit}{plural}";
            }

            return s;
        }

        private static string OverwriteWithScalingBase(string s, Match match)
        {
            var scalingAmount = match.Groups[1].Value;
            if (scalingAmount.Equals("One", StringComparison.OrdinalIgnoreCase))
                scalingAmount = "1";
            
            var scalingUnit = ParseUnit(match.Groups[2].Value);
            int.TryParse(scalingAmount, out var amount);
            var isFeet = scalingUnit.EndsWith("feet", StringComparison.OrdinalIgnoreCase);
            string plural; 
            if (isFeet)
                plural = "";
            else if (amount == 1)
                plural = "(s)";
            else
                plural = "s";   
            

            if (amount == 1)
            {
                return s.Replace(match.Groups[0].Value, $"{ScalingClass} {scalingUnit}{plural}");
            }

            if (amount > 1)
            {
                return s.Replace(match.Groups[0].Value, $"[[{amount}*{ScalingClass} ]] {scalingUnit}{plural}");
            }

            return s;
        }

        private static string ParseUnit(string unit)
        {
            var trim = unit.Trim().TrimEnd('.').TrimEnd('s').Trim();
            trim = ReplaceMatch(trim, @"(rd)$", "round");
            trim = ReplaceMatch(trim, @"(yd)$", "yard");
            trim = ReplaceMatch(trim, @"(hr)$", "hour");
            trim = ReplaceMatch(trim, @"(sq\.?)", "square");
            trim = ReplaceMatch(trim, @"(cu\.?)", "cube");
            trim = ReplaceMatch(trim, @"(ft\.?)(?: \w+)", "foot");
            trim = ReplaceMatch(trim, @"(?:\w+ )?(ft\.?)", "feet");
            trim = ReplaceMatch(trim, @"(ft\.?)", "feet");
            
            return trim;
        }
        
        private static string ReplaceMatch(string trim, string regex, string replace)
        {
            var match = new Regex(regex).Match(trim);
            return !match.Success 
                ? trim 
                : trim.Replace(match.Groups[1].Value, replace);
        }
    }
}
