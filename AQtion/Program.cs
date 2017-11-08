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

        public Field(string path)
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

        // 範囲外を指定されたときにはデフォルト値を返す
        public char GetWithoutException(int x, int y, char invalid=Tile.WALL)
        {
            if (y < 0 ||  lines.Count <= y )
            {
                return invalid;
            }
            if (x < 0 || lines.ElementAt(y).Length <= x) {
                return invalid;
            }
            return Get(x, y);

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
        private bool isGoal;

        public bool End { get => end; }
        public int Score { get => score; }
        public bool IsGoal { get => isGoal; set => isGoal = value; }

        public Simulator(Field field, int screenWidth)
        {
            this.field = field;
            this.screenWidth = screenWidth;
            pos = new Pos(0, 0);
            end = false;
            score = 0;
            turn = 0;
            isGoal = false;
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
                score = 100 + pos.X;
                end = true;
                isGoal = true;
            }
            // 死んだ
            else if (field.Get(pos.X, pos.Y) == Tile.DEATH)
            {
                score = -100 ; // 進むほど死んだときのペナルティがましになるってわけよ
                end = true;
            }
            else
            {
                score = pos.X;
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

        // 現状を文字列として返すハッシュのようなもの
        public string GetState()
        {
            //// 見える画面全部返してたとき
            //List<List<char>> screen = GetScreen();
            //return string.Join("", screen.Select(line => new string(line.ToArray())));

            // 自分の正面HxWを返す
            const int H = 3, W = 3;
            List<char> state = new List<char>();
            for (int y = pos.Y-H/2; y <= pos.Y+H/2; y++)
            {
                for (int x = pos.X; x <= pos.X + W; x++)
                {
                        state.Add(field.GetWithoutException(x, y));
                }
            }

            return string.Join("", state);
        }

        // 報酬を返す
        public double GetReward()
        {
            return score;
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
                (1 - learningRate) * GetValues(state)[action] +
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

            // 評価最大となる選択肢を、偏りがないように *できるだけきれいに* かいたらこうなる
            List<double> vs = GetValues(state);
            List<KeyValuePair<double, int>> qValueWithIndex = new List<KeyValuePair<double, int>>();
            for (int i = 0; i < vs.Count; i++)
            {
                qValueWithIndex.Add(new KeyValuePair<double, int>(vs[i], i));
            }
            return qValueWithIndex.Where(x => x.Key == vs.Max()).OrderBy(x => new Guid()).Take(1).ToArray()[0].Value;
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
        static bool Episode(QValue qValue, Field field, int screenWidth, Random random)
        {
            Simulator simulator = new Simulator(field, 20);
            while (!simulator.End)
            {
                //simulator.Draw();
                //qValue.Show();
                //System.Threading.Thread.Sleep(10);

                string state = simulator.GetState();
                int act = qValue.GetNextAction(state, random);
                simulator.Update((Action)act);

                double reward = simulator.GetReward();
                string nextState = simulator.GetState();
                qValue.Update(state, act, nextState, reward);

            }
            return simulator.IsGoal;
        }
        static void Main(string[] args)
        {
            QValue qValue = new QValue(3, 0.1, 0.3);
            Random random = new Random(100);

            for (int i = 1; i<= 9; i++)
            {
                if (i == 4) { continue; }
                Field field = new Field($"field{i}.txt");
                int goals = 0;
                for (int c = 0; c < 10000; c++)
                {
                    if (Episode(qValue, field, 20, random)) { goals++; }
                }
                Console.WriteLine(goals);
            }

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
