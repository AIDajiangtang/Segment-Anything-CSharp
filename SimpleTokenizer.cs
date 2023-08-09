using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SAMViewer
{
    /// <summary>
    /// BPE
    /// </summary>
    class SimpleTokenizer
    {
        static SimpleTokenizer theSingleton = null;
        Dictionary<int, string> byte_encoder;
        Dictionary<string, int> byte_decoder;
        Dictionary<string, int> encoder = new Dictionary<string, int>();
        Dictionary<int, string> decoder = new Dictionary<int, string>();
        Dictionary<(string, string), int> bpe_ranks = new Dictionary<(string, string), int>();
        Dictionary<string, string> cache = new Dictionary<string, string>();
        System.Text.RegularExpressions.Regex pat;
        int contextLength = 77;

        public static SimpleTokenizer Instance()
        {
            if (null == theSingleton)
            {
                theSingleton = new SimpleTokenizer();
            }
            return theSingleton;
        }

        protected SimpleTokenizer()
        {
            this.Init();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        void Init()
        {
            this.byte_encoder = this.BytesToUnicode();
            this.byte_decoder = this.byte_encoder.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            List<(string, string)> merges = LoadBPEMerges(default_bpe());//加载BPE
            List<string> vocab = this.byte_encoder.Values.ToList();
            foreach (var v in this.byte_encoder.Values.ToList())
            {
                vocab.Add(v + "</w>");
            }


            foreach ((string merge1, string merge2) in merges)
            {
                vocab.Add(merge1 + merge2);
            }
            vocab.AddRange(new List<string> { "<|startoftext|>", "<|endoftext|>" });

            for (int i = 0; i < vocab.Count; i++)
            {
                this.encoder[vocab[i]] = i;
            }
            this.decoder = encoder.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            this.bpe_ranks = merges.Select((merge, index) => new { merge, index })
                                         .ToDictionary(item => item.merge, item => item.index);
            this.cache = new Dictionary<string, string>()
            {
                { "<|startoftext|>","<|startoftext|>" },
                { "<|endoftext|>","<|endoftext|>" }
            };

            this.pat = new Regex(@" <\| startoftext\|>|<\| endoftext\|>| 's|'t | 're|'ve | 'm|'ll | 'd|[\p{L}]+|[\p{N}]|[^\s\p{L}\p{N}]+", RegexOptions.IgnoreCase);
        }
        /// <summary>
        /// 将字符串转换为Token
        /// </summary>
        /// <param name="textpromot"></param>
        /// <returns></returns>
        public List<Int64> tolikenlize(string textpromot)
        {
            int sot_token = this.encoder["<|startoftext|>"];
            int eot_token = this.encoder["<|endoftext|>"];
            List<string> texts = new List<string>() { textpromot };
            List<Int64> allTokens = new List<Int64>();
            foreach (string text in texts)
            {
                allTokens.Add(sot_token);
                allTokens.AddRange(this.Encode(text));
                allTokens.Add(eot_token);
            }
            if (allTokens.Count > contextLength)
            {
                allTokens.RemoveRange(contextLength, allTokens.Count - contextLength);
                allTokens[contextLength-1] = eot_token;
            }
            else
            {
                Int64[] added = new Int64[contextLength - allTokens.Count];
                allTokens.AddRange(added);
            }

            return allTokens;
        }
        /// <summary>
        /// 对字符串进行编码
        /// </summary>
        List<Int64> Encode(string text)
        {
            List<Int64> bpeTokens = new List<Int64>();
            text = this.whitespace_clean(this.basic_clean(text)).ToLower();
            foreach (Match match in Regex.Matches(text, this.pat.ToString()))
            {
                string token = string.Join("", match.Value.Select(c => this.byte_encoder[c]));
                string[] bpeTokenList = this.bpe(token).Split(' ');
                foreach (string bpeToken in bpeTokenList)
                {
                    bpeTokens.Add(this.encoder[bpeToken]);
                }
            }
            return bpeTokens;
        }
        /// <summary>
        /// 将tokens解码成字符串解码
        /// </summary>
        string Decode(List<int> tokens)
        {
            StringBuilder textBuilder = new StringBuilder();
            foreach (int token in tokens)
            {
                textBuilder.Append(this.decoder[token]);
            }
            string text = textBuilder.ToString();

            List<byte> byteList = new List<byte>();
            foreach (char c in text)
            {
                byteList.Add((byte)this.byte_decoder[c.ToString()]);
            }
            byte[] byteArray = byteList.ToArray();
            string decodedText = Encoding.UTF8.GetString(byteArray).Replace("</w>", " ");

            return decodedText;
        }
        string bpe(string token)
        {
            if (cache.ContainsKey(token))
            {
                return cache[token];
            }
            List<string> word = new List<string>();
            for (int i=0;i< token.Length-1;i++)
            {
                word.Add(token[i].ToString());
            }
            word.Add(token[token.Length - 1].ToString() + "</w>");
            //Tuple<string, string> word = Tuple.Create( token[token.Length - 1] + "</w>", token.Substring(0, token.Length - 1));
            HashSet<(string, string)> pairs = this.GetPairs(word);

            if (pairs.Count == 0)
            {
                return token + "</w>";
            }

            while (true)
            {
                (string first, string second) = pairs.OrderBy(pair => bpe_ranks.ContainsKey(pair) ? bpe_ranks[pair] : double.PositiveInfinity).First();
                if (!bpe_ranks.ContainsKey((first, second)))
                {
                    break;
                }

                List<string> newWord = new List<string>();
                int i = 0;
                while (i < word.Count)
                {
                    try
                    {
                        int j = word.IndexOf(first, i);
                        newWord.AddRange(word.GetRange(i, j - i));
                        i = j;
                    }
                    catch
                    {
                        newWord.AddRange(word.GetRange(i, word.Count- i));
                        break;
                    }

                    if (word[i] == first && i < word.Count - 1 && word[i + 1] == second)
                    {
                        newWord.Add(first + second);
                        i += 2;
                    }
                    else {
                        newWord.Add(word[i]);
                        i += 1;
                    }

                }
                word = newWord;
                if (word.Count == 1)
                    break;
                else
                {
                    pairs = this.GetPairs(newWord);
                }
               
            }

            string result = string.Join(" ", word);
            cache[token] = result;
            return result;

        }

        string default_bpe()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)+ "\\bpe_simple_vocab_16e6.txt";
        }
        List<(string, string)> LoadBPEMerges(string bpePath)
        {
            List<(string, string)> merges = new List<(string, string)>();

            using (FileStream fileStream = File.OpenRead(bpePath))
            using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string content = streamReader.ReadToEnd();
                string[] lines = content.Split('\n');
                int startLine = 1;
                int endLine = 49152 - 256 - 2 + 1;
                ArraySegment<string> lineSegment = new ArraySegment<string>(lines, startLine, endLine - startLine);

                foreach (string line in lineSegment)
                {
                    string[] merge = line.Split();
                    merges.Add((merge[0], merge[1]));
                }
            }

            return merges;
        }

        Dictionary<int, string> BytesToUnicode()
        {
            List<int> bs = new List<int>();
            List<int> cs = new List<int>();

            for (int b = (int)'!'; b <= (int)'~'; b++)
            {
                bs.Add(b);
                cs.Add(b);
            }

            for (int b = (int)'¡'; b <= (int)'¬'; b++)
            {
                bs.Add(b);
                cs.Add(b);
            }

            for (int b = (int)'®'; b <= (int)'ÿ'; b++)
            {
                bs.Add(b);
                cs.Add(b);
            }

            int n = 0;
            for (int b = 0; b < 256; b++)
            {
                if (!bs.Contains(b))
                {
                    bs.Add(b);
                    cs.Add(256 + n);
                    n++;
                }
            }

            Dictionary<int, string> byteToUnicode = new Dictionary<int, string>();
            for (int i = 0; i < bs.Count; i++)
            {
                byteToUnicode.Add(bs[i], ((char)cs[i]).ToString());
            }

            return byteToUnicode;
        }
        HashSet<(string, string)> GetPairs(List<string> word)
        {
            HashSet<(string, string)> pairs = new HashSet<(string, string)>();
            string prevChar = word[0];
            for (int i = 1; i < word.Count; i++)
            {
                string currentChar = word[i].ToString();
                pairs.Add((prevChar, currentChar));
                prevChar = currentChar;
            }
            return pairs;
        }

        string HtmlDecode(string text)
        {
            // 还原 HTML 转义字符
            return System.Net.WebUtility.HtmlDecode(text);
        }
        string basic_clean(string text)
        {
            text = HtmlDecode(text);
            return text.Trim();
        }
        string whitespace_clean(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            text = text.Trim();
            return text;
        }
    }
}
