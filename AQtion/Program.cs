using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AQtion
{
    class Pos
    {
        public int X, Y;
        public Pos(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
        public static Pos operator +(Pos p1, Pos p2)
        {
            return new Pos(p1.X + p2.X, p1.Y + p2.Y);
        }
    }
    // 壁や空間などのタイル
    class Tile
    {
        public const char
            SPACE = ' ',
            GOAL = '|',
            WALL = '#',
            DEATH = 'v',
            PLAYER = '&';
    }

    enum Action
    {
        STAY,
        GO,
        JUMP,
    }
    class Field
    {
        private List<string> lines;
        private readonly int width;
        private readonly int height;

        public Field(string path, int height)
        {
            lines = new List<string>();
            width = 0;

            // 読み込み
            using (StreamReader sr = new StreamReader(path))
            {
                while (sr.Peek() >= 0)
                {
                    // 本来は挿入する前に変な文字がないか調べるべき

                    string line = sr.ReadLine();// ReadLine の返り値には\rや\nは含まれない
                    lines.Add(line);
                    width = Math.Max(width, line.Length);
                }
            }

            if (lines.Count > height)
            {
                throw new Exception("field height is too large");
            }
            while (lines.Count < height)
            {
                lines.Insert(0, "");
            }

            // 長さを揃える
            lines = lines.Select(line => line + new string(Tile.SPACE, width - line.Length) + Tile.GOAL).ToList();
            lines.Add(new string(Tile.DEATH, width) + Tile.GOAL);
            // 高さを設定
            this.height = lines.Count;

        }

        public char Get(int x, int y)
        {
            return lines.ElementAt(y).ElementAt(x);
        }

        // この辺りよくわからずに使ってるけど不要でしょって思うよ
        public int Width => width;
        public int Height => height;
    }


    class Simulator
    {
        private Pos pos; // player position
        private Field field;
        private int screenWidth;
        private bool end;
        private int score;
        private int turn;

        public bool End { get => end; }
        public int Score { get => score; }

        public Simulator(Field field, int screenWidth)
        {
            this.field = field;
            this.screenWidth = screenWidth;
            pos = new Pos(0, 0);
            end = false;
            score = 0;
            turn = 0;
        }

        public void Update(Action nextAction)
        {
            turn++;

            // 動く
            switch (nextAction)
            {
                case Action.STAY:
                    break;
                // 進む
                case Action.GO:
                    if (field.Get(pos.X + 1, pos.Y) != Tile.WALL)
                    {
                        pos.X++;
                    }
                    break;
                // ジャンプ
                case Action.JUMP:
                    if (field.Get(pos.X, pos.Y + 1) == Tile.WALL)
                    {
                        // 最大3マスジャンプ
                        for (int i = 0; i <= 3; i++)
                        {
                            if (pos.Y - 1 < 0) { break; } // 画面から出ない
                            if (field.Get(pos.X, pos.Y) != Tile.WALL && // 直感的には pos.Y-1で判定したいけどこの直後に落下処理があるのでこれでうまくいく
                                field.Get(pos.X, pos.Y) != Tile.DEATH)
                            {
                                pos.Y--;
                            }
                        }

                    }
                    break;
            }

            // 落ちる
            if (field.Get(pos.X, pos.Y + 1) != Tile.WALL)
            {
                pos.Y++;
            }


            // ゴール
            if (field.Get(pos.X, pos.Y) == Tile.GOAL)
            {
                score = 100;
                end = true;
            }
            // 死んだ
            else if (field.Get(pos.X, pos.Y) == Tile.DEATH)
            {
                score = -100;
                end = true;
            }
        }

        // 現在の画面を返す
        public List<List<char>> GetScreen()
        {
            List<List<char>> screen = new List<List<char>>();

            // 最初
            if (pos.X <= screenWidth / 2)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    List<char> line = new List<char>();
                    for (int x = 0; x < screenWidth; x++)
                    {
                        line.Add(field.Get(x, y));
                    }
                    screen.Add(line);
                }
                screen[pos.Y][pos.X] = Tile.PLAYER;
            }
            // まんなか
            else if (pos.X < field.Width - screenWidth / 2)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    List<char> line = new List<char>();
                    for (int x = -screenWidth / 2; x < screenWidth / 2; x++)
                    {
                        line.Add(field.Get(pos.X + x, y));
                    }
                    screen.Add(line);
                }
                screen[pos.Y][screenWidth / 2] = Tile.PLAYER;
            }
            // 最後
            else
            {
                for (int y = 0; y < field.Height; y++)
                {
                    List<char> line = new List<char>();
                    for (int x = 0; x <= screenWidth; x++)
                    {
                        line.Add(field.Get(field.Width - screenWidth + x, y));
                    }
                    screen.Add(line);
                }
                screen[pos.Y][pos.X - (field.Width - screenWidth)] = Tile.PLAYER;
            }


            return screen;
        }

        // コンソールに現状を描画
        public void Draw()
        {
            List<List<char>> screen = GetScreen();

            Console.SetCursorPosition(0, 0);
            screen.ForEach(line => Console.WriteLine(line.ToArray()));
        }

        // 現在の画面を一つの文字列として返す
        // 実質ハッシュ関数
        public string GetState()
        {
            List<List<char>> screen = GetScreen();
            return string.Join("", screen.Select(line => new string(line.ToArray())));
        }

        // 報酬を返す
        public double GetReward()
        {
            return (double)score;
        }
    }

    class QValue
    {
        private Dictionary<string, List<double>> values;
        private int actionCount;
        private double learningRate;
        private double discountRate;

        public QValue(int actionCount, double learningRate, double discountRate)
        {
            this.values = new Dictionary<string, List<double>>();
            this.actionCount = actionCount;
            this.learningRate = learningRate;
            this.discountRate = discountRate;
        }
        
        // 辞書で変な例外が出ないようにアクセサ
        private List<double> GetValues(string s)
        {
            if (!values.ContainsKey(s))
            {
                values.Add(s, new List<double>(new double[actionCount]));
            }
            return values[s];
        }
        // Q値を更新
        public void Update(string state, int action, string nextState, double reward)
        {
            GetValues(state)[action] =
                (1 - learningRate) * values[state][action] +
                learningRate * (reward + discountRate * GetValues(nextState).Max());
        }
        // 次の行動を選択
        // e-greedy 法を使ってる
        public int GetNextAction(string state, Random random = null)
        {
            if (random is null)
            {
                random = new Random();
            }
            if (random.NextDouble() < 0.1)
            {
                return random.Next(actionCount);
            }

            var vs = GetValues(state);

            int maxIndex = 0;
            double v = vs.ElementAt(0);
            for (int i = 0; i < vs.Count; i++)
            {
                if (vs.ElementAt(i) > v)
                {
                    v = vs.ElementAt(i);
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        public void Show()
        {
            foreach (var value in values)
            {
                Console.Write(value.Key);
                foreach (var item in value.Value)
                {
                    Console.Write("|{0}", item);
                }
                Console.WriteLine("");
            }
        }

    }

    class Program
    {
        static void Episode(QValue qValue, Field field, int screenWidth, Random random)
        {
            Simulator simulator = new Simulator(field, 20);
            while (!simulator.End)
            {
                //simulator.Draw();
                //System.Threading.Thread.Sleep(10);

                string state = simulator.GetState();
                int act = qValue.GetNextAction(state, random);
                simulator.Update((Action)act);

                double reward = simulator.GetReward();
                string nextState = simulator.GetState();
                qValue.Update(state, act, nextState, reward);
            }
        }
        static void Main(string[] args)
        {
            Field field = new Field("field1.txt", 10);
            QValue qValue = new QValue(3, 0.3, 0.7);
            Random random = new Random();

            for (int i = 0; i < 1000; i++)
            {
                Episode(qValue, field, 20, random);
            }
            qValue.Show();
            Console.ReadKey();

            //Simulator simulator = new Simulator(field, 20);
            //while (!simulator.End)
            //{
            //    simulator.Draw();
            //    var k = Console.ReadKey();
            //    Action nextAction = Action.STAY;

            //    if (k.Key == ConsoleKey.RightArrow)
            //    {
            //        nextAction = Action.GO;
            //    }
            //    else if (k.Key == ConsoleKey.UpArrow)
            //    {
            //        nextAction = Action.JUMP;
            //    }
            //    simulator.Update(nextAction);
            //}
            //Console.Clear();
            //Console.WriteLine("SCORE:{0}", simulator.Score);

        }
    }
}
