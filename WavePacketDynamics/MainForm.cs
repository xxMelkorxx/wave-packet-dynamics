using System;
using System.Windows.Forms;

namespace WavePacketDynamics
{
    public partial class MainForm : Form
    {
        private WPDClass wPD;
        private int iterTime = 1;
        private double df;

        public MainForm()
        {
            InitializeComponent();
        }

        private void OnClickButtonBuild(object sender, EventArgs e)
        {
            // Инициализация системы.
            wPD = new WPDClass(
                new double[] { (double)numUpDown_Ampl.Value, (double)numUpDown_Shift.Value, (double)numUpDown_Sigma.Value },
                new double[] { (double)numUpDown_V.Value, (double)numUpDown_Alpha.Value },
                checkBox_OpenBС.Checked ? (double)numUpDown_a.Value : 1.0,
                checkBox_OpenBС.Checked ? (double)numUpDown_b.Value : 1.0
                );

            // Отрисовка графика волнового пакета.
            chart_WavePacket.Series[0].Points.Clear();
            for (int i = 0; i < wPD.size; i++)
                chart_WavePacket.Series[0].Points.AddXY(-wPD.b + wPD.dx * i, wPD.psiGrid[i, 0].Magnitude);

            chart_WavePacket.ChartAreas[0].AxisX.Minimum = -(checkBox_OpenBС.Checked ? (double)numUpDown_b.Value : 1.0);
            chart_StationaryFunction.ChartAreas[0].AxisX.Minimum = -(checkBox_OpenBС.Checked ? (double)numUpDown_b.Value : 1.0);

            // Отрисовка границ a.
            chart_WavePacket.Series[1].Points.Clear();
            if (checkBox_OpenBС.Checked)
            {
                chart_WavePacket.Series[1].Points.AddXY(-(double)numUpDown_a.Value, 0);
                chart_WavePacket.Series[1].Points.AddXY(-(double)numUpDown_a.Value, chart_WavePacket.ChartAreas[0].AxisY.Maximum);
                chart_WavePacket.Series[1].Points.AddXY((double)numUpDown_a.Value, 0);
                chart_WavePacket.Series[1].Points.AddXY((double)numUpDown_a.Value, chart_WavePacket.ChartAreas[0].AxisY.Maximum);
            }

            iterTime = 1;

            df = 1.0 / (wPD.time * wPD.dt);
            trackBar_Freq.Maximum = (int)(df * wPD.time);
            trackBar_Node.Maximum = wPD.size;
            
            button_Solve.Enabled = true;
        }

        private void OnClickButtonSolve(object sender, EventArgs e)
        {
            // Решение методом сеточной прогонки.
            wPD.Solve(checkBox_OpenBС.Checked);

            // Получение набора спектров по всем узлам.

            if (!timer.Enabled)
            {
                timer.Start();
                button_Solve.Text = "Стоп";
                button_Build.Enabled = false;
                checkBox_OpenBС.Enabled = false;
                trackBar_Node.Enabled = true;
                numUpDown_Ampl.Enabled = false;
                numUpDown_Shift.Enabled = false;
                numUpDown_Sigma.Enabled = false;
                trackBar_Freq.Enabled = true;
                OnScrollStationaryFunction(null, null);
            }
            else
            {
                timer.Stop();
                button_Solve.Text = "Старт";
                button_Build.Enabled = true;
                checkBox_OpenBС.Enabled = true;
                numUpDown_Ampl.Enabled = true;
                numUpDown_Shift.Enabled = true;
                numUpDown_Sigma.Enabled = true;
            }
        }

        private void OnTickTimer(object sender, EventArgs e)
        {
            // Отрисовка графика волнового пакета.
            chart_WavePacket.Series[0].Points.Clear();
            for (int i = 0; i < wPD.size; i++)
                chart_WavePacket.Series[0].Points.AddXY(-wPD.b + wPD.dx * i, wPD.psiGrid[i, iterTime].Magnitude);

            if (iterTime == 1)
                OnScrollTrackBarNode(null, null);

            iterTime++;
            if (iterTime >= wPD.time)
            {
                OnClickButtonSolve(null, null);
                button_Solve.Enabled = false;
            }
        }

        private void OnScrollTrackBarNode(object sender, EventArgs e)
        {
            int node = (int)trackBar_Node.Value;
            label_Node.Text = node.ToString();

            // Отрисовка графика модуля спектра.
            chart_ModuleSpectrum.Series[0].Points.Clear();
            for (int i = 0; i < wPD.time; i++)
                chart_ModuleSpectrum.Series[0].Points.AddXY(df * i, wPD.spectrums[node][i].Magnitude);
        }

        private void OnScrollStationaryFunction(object sender, EventArgs e)
        {
            int freq = (int)trackBar_Freq.Value;
            double[] statFunc = wPD.GetStationaryFunction(freq);

            label_Freq.Text = freq.ToString();
            chart_StationaryFunction.Series[0].Points.Clear();
            chart_ModuleSpectrum.Series[1].Points.Clear();
            for (int i = 0; i < wPD.size; i++)
                chart_StationaryFunction.Series[0].Points.AddXY(-wPD.b + i * wPD.dx, statFunc[i]);

            chart_ModuleSpectrum.Series[1].Points.AddXY(freq, 0);
            chart_ModuleSpectrum.Series[1].Points.AddXY(freq, 10);
        }

        private void OnCkeckedChangedOpenBC(object sender, EventArgs e)
        {
            if (checkBox_OpenBС.Checked)
            {
                numUpDown_a.Enabled = true;
                numUpDown_b.Enabled = true;
            }
            else
            {
                numUpDown_a.Enabled = false;
                numUpDown_b.Enabled = false;
            }
            OnClickButtonBuild(null, null);
        }

        private void OnValueChangedNUD_A(object sender, EventArgs e)
        {
            numUpDown_a.Maximum = numUpDown_b.Value;
            OnClickButtonBuild(null, null);
        }

        private void OnValueChangedNUD_B(object sender, EventArgs e)
        {
            numUpDown_b.Minimum = numUpDown_a.Value;
            chart_WavePacket.ChartAreas[0].AxisX.Minimum = -(double)numUpDown_b.Value;
            OnClickButtonBuild(null, null);
        }
    }
}