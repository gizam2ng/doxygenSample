using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConvertDotToNode
{
    class Program
    {
        public static Dictionary<string, NodeClass> nodeDic = new Dictionary<string, NodeClass>();

        public static DataTable dt = new DataTable();

        /***
         * 事前にDoxygen + Graphviz でメソッド呼び出しのdotファイルを出力しておく
         * dotファイル内のノード（Node）はメソッド
         */
        static void Main(string[] args)
        {
            // CSV出力の準備
            dt.Columns.Add("名前空間");
            dt.Columns.Add("クラス名");
            dt.Columns.Add("メソッド名");
            dt.Columns.Add("完全名");

            // 引数に指定されたフォルダ配下のdotファイルを読み込む
            IEnumerable<string> dotfiles = System.IO.Directory.EnumerateFiles(args[0], "*.dot", System.IO.SearchOption.AllDirectories);

            StreamReader sr;

            foreach (string file in dotfiles)
            {
                Console.WriteLine("dotファイル名：" + file);

                // dotファイルをUTF8で読み込む
                sr = new StreamReader(file, Encoding.UTF8);

                // dotファイルごとにノードを格納するディクショナリを初期化する
                nodeDic = new Dictionary<string, NodeClass>();

                while (sr.Peek() != -1)
                {
                    // dotファイルを一行ごと読み込む
                    string txt = sr.ReadLine();

                    // 空白で分割する
                    string[] txtarray = txt.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);

                    // ノードの定義だけを取得するため、Node[0-9]で始まらない場合は次の行へ
                    if (!Regex.IsMatch(txtarray[0], "^Node[0-9]?"))
                    {
                        continue;
                    }
                    Console.WriteLine(txtarray[0]);

                    // ノードの定義がディクショナリに存在しなければ作成する
                    NodeClass node;
                    if (!nodeDic.ContainsKey(txtarray[0]))
                    {
                        node = new NodeClass();
                        nodeDic.Add(txtarray[0], node);
                    }
                    // 存在すればノード定義を取り出す
                    else
                    {
                        node = nodeDic[txtarray[0]];
                    }

                    // 2つ目をカンマで分割する
                    string[] txtarray2 = txtarray[1].Split(',');

                    // "->"の場合、ノードの呼び出し先を格納する
                    if (txtarray2[0] == "->")
                    {
                        Console.WriteLine(txtarray2[0]);
                        Console.WriteLine(txtarray[2]);
                        node.callList.Add(txtarray[2]);
                    }

                    // "[label="で始まる場合、ノードのメソッド名が定義されているため、メソッド名を切り出して格納する
                    if (Regex.IsMatch(txtarray2[0], @"\[label="))
                    {
                        Console.WriteLine(txtarray2[0]);
                        // ダブルウォーテーションで分割
                        string[] txtarray3 = txtarray2[0].Split('"');
                        Console.WriteLine(txtarray3[0]);
                        Console.WriteLine(txtarray3[1]);
                        Console.WriteLine(txtarray3[1].Replace(@"\l", ""));
                        node.name = txtarray3[1].Replace(@"\l", "");
                    }
                    //}
                }
                sr.Close();

                Console.WriteLine("*****");
                int depthcnt = 0;
                foreach (string nodeKey in nodeDic.Keys)
                {
                    Console.WriteLine("*****" + depthcnt++);
                    print(nodeKey, 0);
                }
            }

            //CSV出力用変数の作成
            List<string> lines = new List<string>();

            //列名をカンマ区切りで1行に連結
            List<string> header = new List<string>();
            foreach (DataColumn dr in dt.Columns)
            {
                header.Add(dr.ColumnName);
            }
            lines.Add(string.Join(",", header));

            //列の値をカンマ区切りで1行に連結
            foreach (DataRow dr in dt.Rows)
            {
                lines.Add(string.Join(",", dr.ItemArray));
            }

            // CSV出力
            WriteCsv(args[0] + "sample.csv", lines);

            Console.ReadKey();
        }

        public static void print(string nodekey, int depthcnt)
        {
            //Console.WriteLine(cnt);
            string strtabs = "";
            //for (int i = 0; i < count; i++)
            for (int i = 0; i < depthcnt; i++)
            {
                strtabs = strtabs + "    ";
            }
            //Console.WriteLine(strtabs + "Node:" + nodeDic[nodekey].name);
            string output = nodeDic[nodekey].name;
            Console.WriteLine(strtabs + output);

            // CSV出力行
            string[] outputarray = output.Split('.');
            string outputnamespacetemp = output.Substring(0, output.LastIndexOf('.'));
            string outputnamespace = outputnamespacetemp.Substring(0, outputnamespacetemp.LastIndexOf('.'));
            string outputclass = outputarray[outputarray.Length - 2];
            string outputmethod = outputarray[outputarray.Length - 1];
            Console.WriteLine(strtabs + "名前空間：" + outputnamespace);
            Console.WriteLine(strtabs + "クラス名：" + outputclass);
            Console.WriteLine(strtabs + "メソッド名：" + outputmethod);
            Console.WriteLine(strtabs + "完全名：" + output);
            dt.Rows.Add(new string[] { strtabs + outputnamespace, strtabs + outputclass, strtabs + outputmethod, strtabs + output });

            List<string> list = nodeDic[nodekey].callList;

            ++depthcnt;

            foreach (string call in list)
            {
                //Console.WriteLine(strtabs + "Call:" + call);
                print(call, depthcnt);
            }
        }

        private static void WriteCsv(string filename, IEnumerable<string> lines, bool append = false, string encode = "shift-jis")
        {
            using (StreamWriter sw = new StreamWriter(filename, append, Encoding.GetEncoding(encode)))
            {
                foreach (string line in lines)
                {
                    sw.WriteLine(line);
                }
            }
        }
    }

    public class NodeClass
    {
        public string name;
        public List<string> callList = new List<string>();
    }
}
