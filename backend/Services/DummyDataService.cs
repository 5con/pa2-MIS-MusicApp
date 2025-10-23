using System;
namespace FreelanceMusicPlatform.Services
{
    public class DummyDataService
    {
        private readonly DatabaseService _databaseService;

        public DummyDataService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public (int Teachers, int Students, int Lessons) Generate(int teachers, int students, int lessons)
        {
            return _databaseService.SeedDummyData(teachers, students, lessons);
        }

        public void Clear(bool preserveAdmin = true)
        {
            _databaseService.ClearAllData(preserveAdmin);
        }
    }
}


