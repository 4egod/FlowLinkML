using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowLinkML.Models
{
    public class MLModel
    {
        public class Output
        {
            public float Score { get; set; }
        }

        public class Metrics
        {
            public double MeanAbsoluteError { get; set; }

            public double MeanSquaredError { get; set; }
        }

        private MLContext _context;

        private List<Archive> _trainingData;

        private EstimatorChain<RegressionPredictionTransformer<FastTreeRegressionModelParameters>> _pipeline;

        private PredictionEngine<Archive, Output> _engine;

        public MLModel(List<Archive> trainingData, List<string> features)
        {
            _trainingData = trainingData;

            _context = new MLContext(seed: 1);
            
            var dataProcessPipeline = _context.Transforms.Text.FeaturizeText("Timestamp_tf", "Timestamp")
                          .Append(_context.Transforms.Concatenate("Features", features.ToArray()));

            var trainer = _context.Regression.Trainers.FastTree(labelColumnName: "Volume", featureColumnName: "Features");

            _pipeline = dataProcessPipeline.Append(trainer);
        }

        public MLModel(string fileName)
        {
            _context = new MLContext();
            string modelPath = AppDomain.CurrentDomain.BaseDirectory + $"./data/{fileName}";
            ITransformer mlModel = _context.Model.Load(modelPath, out var modelInputSchema);
            _engine = _context.Model.CreatePredictionEngine<Archive, Output>(mlModel);
        }

        public void Create(string fileName)
        {
            string modelPath = AppDomain.CurrentDomain.BaseDirectory + $"./data/{fileName}";
            IDataView trainingDataView = _context.Data.LoadFromEnumerable<Archive>(_trainingData);
            ITransformer model = _pipeline.Fit(trainingDataView);
            _engine = _context.Model.CreatePredictionEngine<Archive, Output>(model);

            _context.Model.Save(model, trainingDataView.Schema, modelPath);
        }

        public Metrics Validate()
        {
            IDataView trainingDataView = _context.Data.LoadFromEnumerable<Archive>(_trainingData);

            var crossValidationResults = _context.Regression.CrossValidate(trainingDataView, _pipeline, numberOfFolds: 5, labelColumnName: "Volume");

            Metrics res = new Metrics()
            {
                MeanAbsoluteError = crossValidationResults.Select(r => r.Metrics.MeanAbsoluteError).Average(),
                MeanSquaredError = crossValidationResults.Select(r => r.Metrics.MeanSquaredError).Average(),
            };

            return res;
        }

        public float Predict(Archive value)
        {
            Output result = _engine.Predict(value);

            return result.Score;
        }
    }
}
