using System;
using System.Threading.Tasks;
using ELTE.Sudoku.Persistence;

namespace ELTE.Sudoku.Model
{
    /// <summary>
    /// Játéknehézség felsorolási típusa.
    /// </summary>
    public enum GameDifficulty { Easy, Medium, Hard }

    /// <summary>
    /// Sudoku játék típusa.
    /// </summary>
    public class SudokuGameModel
    {
        #region Difficulty constants

        private const Int32 GameTimeEasy = 3600;
        private const Int32 GameTimeMedium = 1200;
        private const Int32 GameTimeHard = 600;
        private const Int32 GeneratedFieldCountEasy = 6;
        private const Int32 GeneratedFieldCountMedium = 12;
        private const Int32 GeneratedFieldCountHard = 18;

        #endregion

        #region Fields

        private ISudokuDataAccess _dataAccess; // adatelérés
        private SudokuTable _table; // játéktábla
        private GameDifficulty _gameDifficulty; // nehézség
        private Int32 _gameStepCount; // lépések száma
        private Int32 _gameTime; // játékidő

        #endregion

        #region Properties

        /// <summary>
        /// Lépések számának lekérdezése.
        /// </summary>
        public Int32 GameStepCount { get { return _gameStepCount; } }

        /// <summary>
        /// Hátramaradt játékidő lekérdezése.
        /// </summary>
        public Int32 GameTime { get { return _gameTime; } }

        /// <summary>
        /// Játéktábla lekérdezése.
        /// </summary>
        public SudokuTable Table { get { return _table; } }

        /// <summary>
        /// Játék végének lekérdezése.
        /// </summary>
        public Boolean IsGameOver  { get { return (_gameTime == 0 || _table.IsFilled); } }

        /// <summary>
        /// Játéknehézség lekérdezése, vagy beállítása.
        /// </summary>
        public GameDifficulty GameDifficulty { get { return _gameDifficulty; } set { _gameDifficulty = value; } }

        #endregion

        #region Events

        /// <summary>
        /// Játék előrehaladásának eseménye.
        /// </summary>
        public event EventHandler<SudokuEventArgs> GameAdvanced;

        /// <summary>
        /// Játék végének eseménye.
        /// </summary>
        public event EventHandler<SudokuEventArgs> GameOver;

        #endregion

        #region Constructor

        /// <summary>
        /// Sudoku játék példányosítása.
        /// </summary>
        /// <param name="dataAccess">Az adatelérés.</param>
        public SudokuGameModel(ISudokuDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
            _table = new SudokuTable();
            _gameDifficulty = GameDifficulty.Medium;
        }

        #endregion

        #region Public game methods

        /// <summary>
        /// Új játék kezdése.
        /// </summary>
        public void NewGame()
        {
            _table = new SudokuTable();
            _gameStepCount = 0;

            switch (_gameDifficulty) // nehézségfüggő beállítása az időnek, illetve a generált mezőknek
            { 
                case GameDifficulty.Easy:
                    _gameTime = GameTimeEasy;
                    GenerateFields(GeneratedFieldCountEasy);
                    break;
                case GameDifficulty.Medium:
                    _gameTime = GameTimeMedium;
                    GenerateFields(GeneratedFieldCountMedium);
                    break;
                case GameDifficulty.Hard:
                    _gameTime = GameTimeHard;
                    GenerateFields(GeneratedFieldCountHard);
                    break;
            }
        }

        /// <summary>
        /// Játékidő léptetése.
        /// </summary>
        public void AdvanceTime()
        {
            if (IsGameOver) // ha már vége, nem folytathatjuk
                return;

            _gameTime--;
            OnGameAdvanced();

            if (_gameTime == 0) // ha lejárt az idő, jelezzük, hogy vége a játéknak
                OnGameOver(false);
        }


        /// <summary>
        /// Táblabeli lépés végrehajtása.
        /// </summary>
        /// <param name="x">Vízszintes koordináta.</param>
        /// <param name="y">Függőleges koordináta.</param>
        public void Step(Int32 x, Int32 y)
        {
            if (IsGameOver) // ha már vége a játéknak, nem játszhatunk
                return;
            if (_table.IsLocked(x, y)) // ha a mező zárolva van, nem léthatünk
                return;

            _table.StepValue(x, y);

            _gameStepCount++; // lépésszám növelés

            OnGameAdvanced();

            if (_table.IsFilled) // ha vége a játéknak, jelezzük, hogy győztünk
            {
                OnGameOver(true);
            }
        }

        /// <summary>
        /// Játék betöltése.
        /// </summary>
        /// <param name="path">Elérési útvonal.</param>
        public async Task LoadGameAsync(String path)
        {
            if (_dataAccess == null)
                throw new InvalidOperationException("No data access is provided.");

            _table = await _dataAccess.LoadAsync(path);
            _gameStepCount = 0;

            switch (_gameDifficulty) // játékidő beállítása
            {
                case GameDifficulty.Easy:
                    _gameTime = GameTimeEasy;
                    break;
                case GameDifficulty.Medium:
                    _gameTime = GameTimeMedium;
                    break;
                case GameDifficulty.Hard:
                    _gameTime = GameTimeHard;
                    break;
            }
        }

        /// <summary>
        /// Játék mentése.
        /// </summary>
        /// <param name="path">Elérési útvonal.</param>
        public async Task SaveGameAsync(String path)
        {
            if (_dataAccess == null)
                throw new InvalidOperationException("No data access is provided.");

            await _dataAccess.SaveAsync(path, _table);
        }

        #endregion

        #region Private game methods

        /// <summary>
        /// Mezők generálása.
        /// </summary>
        /// <param name="count">Mezők száma.</param>
        private void GenerateFields(Int32 count)
        {
            Random random = new Random();

            for (Int32 i = 0; i < count; i++)
            {
                Int32 x, y;

                do
                {
                    x = random.Next(_table.Size);
                    y = random.Next(_table.Size);
                }
                while (!_table.IsEmpty(x, y)); // üres mező véletlenszerű kezelése

                do
                {
                    for (Int32 j = random.Next(10) + 1; j >= 0; j--) // véletlenszerű növelés
                    {
                        _table.StepValue(x, y);
                    }
                }
                while (_table.IsEmpty(x, y));

                _table.SetLock(x, y);
            }
        }

        #endregion

        #region Private event methods

        /// <summary>
        /// Játékidő változás eseményének kiváltása.
        /// </summary>
        private void OnGameAdvanced()
        {
            if (GameAdvanced != null)
                GameAdvanced(this, new SudokuEventArgs(false, _gameStepCount, _gameTime));
        }

        /// <summary>
        /// Játék vége eseményének kiváltása.
        /// </summary>
        /// <param name="isWon">Győztünk-e a játékban.</param>
        private void OnGameOver(Boolean isWon)
        {
            if (GameOver != null)
                GameOver(this, new SudokuEventArgs(isWon, _gameStepCount, _gameTime));
        }

        #endregion
    }
}
