namespace Allegro.CosmosDb.Migrator.Application.Migrations.Processor
{
    public class RuConsumptionCalculator
    {
        private readonly int _maxRu;

        private double _currentRuConsumption = 0;
        private double _currentRuConsumptionPerDoc;
        private int _currentDocumentCountInBatch;

        public RuConsumptionCalculator(int maxRu, int initialDocumentCountInBatch)
        {
            _maxRu = maxRu;
            _currentDocumentCountInBatch = initialDocumentCountInBatch;
        }

        public void SetCurrentRuConsumption(int documentCount, double ruUsed)
        {
            _currentDocumentCountInBatch = documentCount;
            _currentRuConsumption = ruUsed;
            _currentRuConsumptionPerDoc = ruUsed / documentCount;
        }

        private bool IsConfigured => _maxRu != 0 && _currentRuConsumption != 0;

        public int CalculateMaxDocumentPerSecond()
        {
            return IsConfigured ? (int)(_maxRu / _currentRuConsumptionPerDoc) : _currentDocumentCountInBatch;
        }
    }
}