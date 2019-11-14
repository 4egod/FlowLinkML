using InteractiveDataDisplay.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlowLinkML
{
    using Models;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Archive> trainData;

        private List<Archive> predictData;

        private MLModel _mlModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void train_load_Click(object sender, RoutedEventArgs e)
        {
            int deviceId = int.Parse(train_device.Text);
            int month = train_month.SelectedIndex + 1;

            var task = Task.Run(() =>
            {
                using (AppContext db = new AppContext())
                {
                    trainData = db.FlowmeterHourlyArchive.Where(x => x.DeviceId == deviceId &&
                      x.Timestamp >= DateTime.Parse($"{month}/01/2019") &&
                      x.Timestamp < DateTime.Parse($"{month+1}/01/2019")).OrderBy(x => x.Timestamp).ToList();
                }
            });

            await Process(task);

            Draw(trainData.Select(x => x.Volume), "Actual");
            Log(trainData);
        }

        private async void export_csv_Click(object sender, RoutedEventArgs e)
        {
            int deviceId = int.Parse(train_device.Text);
            var fileName = $"./data/{deviceId}.csv";

            File.Delete(fileName);
            string line = $"Timestamp,DifferentialPressure,Pressure,Temperature,Volume,FlowTime\r\n";
            File.AppendAllText(fileName, line);

            var task = Task.Run(() =>
            {
                foreach (var item in trainData)
                {
                    line = $"{item.Timestamp},{item.DifferentialPressure},{item.Pressure},{item.Temperature},{item.Volume},{item.FlowTime}\r\n";
                    File.AppendAllText(fileName, line);
                }
            });

            await Process(task);
            Log($"File {fileName} has been created.");
        }

        private async void import_csv_Click(object sender, RoutedEventArgs e)
        {
            trainData = new List<Archive>();
            int deviceId = int.Parse(train_device.Text);
            var fileName = $"./data/{deviceId}.csv";

            var task = Task.Run(async() =>
            {
                await Task.Delay(200);
                
                var lines = await File.ReadAllLinesAsync(fileName);

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Split(',');
                    trainData.Add(new Archive()
                    {
                        DeviceId = deviceId,
                        Timestamp = DateTime.Parse(values[0]),
                        DifferentialPressure = float.Parse(values[1]),
                        Pressure = float.Parse(values[2]),
                        Temperature = float.Parse(values[3]),
                        Volume = float.Parse(values[4]),
                        FlowTime = float.Parse(values[5])
                    });
                }
            });

            await Process(task);

            Draw(trainData.Select(x => x.Volume), "Actual");
            Log(trainData);
        }

        private async void train_model_Click(object sender, RoutedEventArgs e)
        {
            int deviceId = int.Parse(train_device.Text);

            List<string> features = new List<string>();
            features.Add("Timestamp_tf");
            if (cb_dp.IsChecked.Value)
            {
                features.Add("DifferentialPressure");
            }

            if (cb_p.IsChecked.Value)
            {
                features.Add("Pressure");
            }

            if (cb_t.IsChecked.Value)
            {
                features.Add("Temperature");
            }

            if (cb_ft.IsChecked.Value)
            {
                features.Add("FlowTime");
            }

            _mlModel = new MLModel(trainData, features);

            var task = Task.Run(async() =>
            {
                _mlModel.Create($"{deviceId}.mlm");
                await Task.Delay(500);
            });

            await Process(task);

            Log($"Model ./data/{deviceId}.mlm has been created.");
        }

        private async void train_test_Click(object sender, RoutedEventArgs e)
        {
            List<float> predictions = new List<float>();

            MLModel.Metrics metrics = null;

            var task = Task.Run(() =>
            {
                for (int i = 0; i < trainData.Count; i++)
                {
                    var prediction = _mlModel.Predict(trainData[i]);
                    predictions.Add(prediction);
                }

                metrics = _mlModel.Validate();
            });

            await Process(task);

            DrawChild(predictions, "Prediction");

            Log($"ML model metrics:\n" +
                $"Mean absolute error: {metrics.MeanAbsoluteError:f2}\n" +
                $"Mean squared error: {metrics.MeanSquaredError:f2}");

        }

        private async void predict_load_Click(object sender, RoutedEventArgs e)
        {                      
            var date = predict_calendar.SelectedDate;
            int deviceId = int.Parse(train_device.Text);
            
            var task = Task.Run(() =>
            {
                using (AppContext db = new AppContext())
                {
                    predictData = db.FlowmeterHourlyArchive.Where(x => x.DeviceId == deviceId &&
                        x.Timestamp >= date.Value.Date && x.Timestamp < date.Value.Date.AddDays(1)).OrderBy(x=>x.Timestamp).ToList();
                }
            });

            await Process(task);

            Draw(predictData.Select(x => x.Volume), "Actual");
            Log(predictData);
        }

        private async void export_predict_csv_Click(object sender, RoutedEventArgs e)
        {
            int deviceId = int.Parse(train_device.Text);
            var fileName = $"./data/{deviceId}_predict.csv";

            File.Delete(fileName);
            string line = $"Timestamp,DifferentialPressure,Pressure,Temperature,Volume,FlowTime\r\n";
            File.AppendAllText(fileName, line);

            var task = Task.Run(() =>
            {
                foreach (var item in predictData)
                {
                    line = $"{item.Timestamp},{item.DifferentialPressure},{item.Pressure},{item.Temperature},{item.Volume},{item.FlowTime}\r\n";
                    File.AppendAllText(fileName, line);
                }
            });

            await Process(task);
            Log($"File {fileName} has been created.");
        }

        private async void import_predict_csv_Click(object sender, RoutedEventArgs e)
        {
            predictData = new List<Archive>();
            int deviceId = int.Parse(train_device.Text);
            var fileName = $"./data/{deviceId}_predict.csv";

            var task = Task.Run(async () =>
            {
                await Task.Delay(200);

                var lines = await File.ReadAllLinesAsync(fileName);

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Split(',');
                    predictData.Add(new Archive()
                    {
                        DeviceId = deviceId,
                        Timestamp = DateTime.Parse(values[0]),
                        DifferentialPressure = float.Parse(values[1]),
                        Pressure = float.Parse(values[2]),
                        Temperature = float.Parse(values[3]),
                        Volume = float.Parse(values[4]),
                        FlowTime = float.Parse(values[5])
                    });
                }
            });

            await Process(task);

            Draw(predictData.Select(x => x.Volume), "Actual");
            Log(predictData);
        }

        private async void predict_predict_Click(object sender, RoutedEventArgs e)
        {
            List<float> predictions = new List<float>();

            int deviceId = int.Parse(train_device.Text);

            string s = $"Count: {predictData.Count()}\n";

            var task = Task.Run(() =>
            {
                MLModel model = new MLModel($"{deviceId}.mlm");

                for (int i = 0; i < predictData.Count; i++)
                {
                    var prediction = model.Predict(predictData[i]);
                    predictions.Add(prediction);
                    s += $"{predictData[i].Timestamp.ToString("HH")}: {predictData[i].Volume:f1} -> {prediction:f1}" +
                    $" ({((predictData[i].Volume != 0) ? 100 - predictData[i].Volume / prediction * 100:0):f2}%)\n";
                }
            });

            await Process(task);

            DrawChild(predictions, "Prediction");
            Log(s);
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            predict_calendar.SelectedDate = DateTime.Now;

            if (!Directory.Exists("./data"))
            {
                Directory.CreateDirectory("./data");
            }
        }

        private void Draw(IEnumerable<float> values, string description)
        {
            var line = new LineGraph();
            train_chart.Content = line;
            line.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            line.StrokeThickness = 2;
            line.Description = description;
            line.PlotY(values);
        }

        private void DrawChild(IEnumerable<float> values, string description)
        {
            var line = new LineGraph();
            Plot plot = train_chart.Content as Plot;
            if (plot.Children.Count > 1)
            {
                plot.Children.RemoveAt(1);
            }

            plot.Children.Add(line);
            line.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 255));
            line.StrokeThickness = 2;
            line.Description = description;
            line.PlotY(values);
        }

        private void Log(List<Archive> values)
        {
            string s = $"Count: {values.Count()}\n"; 

            foreach (var item in values)
            {
                s += $"{item.Timestamp.ToString("dd HH:mm:ss")}: {item.Volume}\n";
            }

            log.Text = s;
        }

        private void Log(string value)
        {
            log.Text = value;
        }

        private async Task Process(Task task)
        {
            WaitWindow waitWindow = new WaitWindow();
            waitWindow.TaskHandler = task;
            waitWindow.ShowDialog();
            await task;
        }

        private void predict_calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            Mouse.Capture(null);
        }


    }
}
