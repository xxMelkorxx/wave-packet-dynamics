using System;
using System.Numerics;
using System.Collections.Generic;

namespace WavePacketDynamics
{
    class WPDClass
    {
        /// <summary>
        /// Число узлов.
        /// </summary>
        public int size;
        /// <summary>
        /// Число временных шагов.
        /// </summary>
        public int time;
        /// <summary>
        /// Шаг по времени.
        /// </summary>
        public double dt;
        /// <summary>
        /// Шаг по Х.
        /// </summary>
        public double dx;

        // Граничные условия.
        /// <summary>
        /// Левое граничное условие.
        /// </summary>
        private double leftBC;
        /// <summary>
        /// Правое граничное условие.
        /// </summary>
        private double rigthBC;

        // Открытые граничные условия.
        /// <summary>
        /// Ближняя граница.
        /// </summary>
        public double a;
        /// <summary>
        /// Дальняя граница.
        /// </summary>
        public double b;

        // Параметры гауссова волнового пакета.
        private double ampl;
        private double shift;
        private double sigma;

        // Параметры потенциала.
        private double V;
        private double alpha;

        /// <summary>
        /// Сетка узлов.
        /// </summary>
        public Complex[,] psiGrid;

        /// <summary>
        /// Массив коэффициентов альф.
        /// </summary>
        private Complex[] alphaArray;
        /// <summary>
        /// Массив коэффициентов бет.
        /// </summary>
        private Complex[] betaArray;

        /// <summary>
        /// Массив спектров.
        /// </summary>
        public Complex[][] spectrums;

        private readonly Complex j = Complex.ImaginaryOne;

        public WPDClass(double[] paramWavePacket, double[] paramPotential, double a = 1, double b = 1, int time = 1024, int size = 500)
        {
            // Инициализация граничных условий.
            leftBC = 0;
            rigthBC = 0;
            // Инициализация параметров потенциала.
            V = paramPotential[0];
            alpha = paramPotential[1];
            // Инициализация параметров гауссова волнового пакета.
            ampl = paramWavePacket[0];
            shift = paramWavePacket[1];
            sigma = paramWavePacket[2];

            this.a = a;
            this.b = b;
            this.size = size + 1;
            this.time = time;

            dx = 2 * this.b / this.size;
            dt = 0.002;//Math.Pow(dx, 2) * 3;

            psiGrid = new Complex[this.size, this.time];
            // Инициализация начального волнового пакета.
            for (int i = 0; i < this.size; i++)
                psiGrid[i, 0] = GaussianWavePacket(-this.b + i * dx);

            alphaArray = new Complex[this.size];
            betaArray = new Complex[this.size];
        }

        /// <summary>
        /// Решение методом сеточной прогонки.
        /// </summary>
        /// <param name="isOpenBC">С учётом открытых граничных условий.</param>
        public void Solve(bool isOpenBC = false)
        {
            Complex Ak, Bk, Ck, Dk;
            for (int i = 1; i < time - 1; i++)
            {
                // Прямой ход прогонки.
                for (int k = 1; k < size - 1; k++)
                {
                    double xk = -b + k * dx;
                    double uk = Potential(xk);
                    if (!isOpenBC)
                    {
                        Ak = -j * dt / (2 * dx * dx);
                        Bk = -j * dt / (2 * dx * dx);
                        Ck = 1 + j * dt * (uk / 2 + 1 / (dx * dx));
                        Dk = psiGrid[k, i - 1] + j * dt / 2 * ((psiGrid[k + 1, i - 1] - 2 * psiGrid[k, i - 1] + psiGrid[k - 1, i - 1]) / (dx * dx) - uk * psiGrid[k, i - 1]);
                    }
                    else
                    {
                        Ak = -j * dt * F(xk) * F(xk - dx) / (2 * dx * dx);
                        Bk = -j * dt * F(xk) * F(xk + dx) / (2 * dx * dx);
                        Ck = 1 + j * dt / 2 * (uk + F(xk) / (dx * dx) * (F(xk + dx) + F(xk - dx)));
                        Dk = (1 - j * dt * uk / 2) * psiGrid[k, i - 1] + j * dt * F(xk)
                            * (F(xk + dx) * psiGrid[k + 1, i - 1] - (F(xk + dx) + F(xk - dx)) * psiGrid[k, i - 1] + F(xk - dx) * psiGrid[k - 1, i - 1]) / (2 * dx * dx);
                    }

                    alphaArray[k] = -Bk / (Ck + Ak * alphaArray[k - 1]);
                    betaArray[k] = (Dk - Ak * betaArray[k - 1]) / (Ck + Ak * alphaArray[k - 1]);
                }

                psiGrid[size - 1, i] = (leftBC * betaArray[size - 1] + rigthBC) / (1 - leftBC * alphaArray[size - 1]);

                // Обратынй ход прогонки.
                for (int k = size - 2; k >= 0; k--)
                    psiGrid[k, i] = alphaArray[k] * psiGrid[k + 1, i] + betaArray[k];
            }

            GetSpectrums();
        }

        /// <summary>
        /// Получить набор спектров собственных значений энергии.
        /// </summary>
        /// <returns></returns>
        private void GetSpectrums()
        {
            Complex[] wavePacket = new Complex[time];
            spectrums = new Complex[size][];

            for (int k = 0; k < size; k++)
            {
                for (int i = 0; i < time; i++)
                    wavePacket[i] = psiGrid[k, i];
                spectrums[k] = FFTClass.FFT(wavePacket);
            }
        }

        public double[] GetStationaryFunction(int freq)
        {
            double[] wavePacket = new double[size];
            for (int i = 0; i < size; i++)
                wavePacket[i] = spectrums[i][freq].Magnitude;

            return wavePacket;
        }

        /// <summary>
        /// Гауссов волновой пакет.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private double GaussianWavePacket(double x)
        {
            return ampl * Math.Exp(-Math.Pow((x - shift) / 2 * sigma, 2));
        }

        /// <summary>
        /// Функция потенциала.
        /// </summary>
        /// <param name="x">Пространственная переменная.</param>
        /// <returns></returns>
        private double Potential(double x)
        {
            return (Math.Abs(x) < a) ? -V / Math.Pow(Math.Cosh(alpha * x), 2) : 0;
        }

        private Complex F(double x, double g = 1)
        {
            if (x >= a) return Complex.One / (1 + j * g * Math.Pow(x - a, 2));
            else if (x <= -a) return Complex.One / (1 + j * g * Math.Pow(x + a, 2));
            else return Complex.One;
        }
    }
}