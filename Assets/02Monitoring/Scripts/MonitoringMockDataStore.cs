using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MockEquipData
{
    public string id;
    public string name;
    public string model;
    public string location;
}

public class MockTechData
{
    public string id;
    public string name;
    public string role;
    public string department;
}

public class MockManualData
{
    public string id;
    public string equipmentId;
    public string title;
    public string category;
}

public static class MonitoringMockDataStore
{
    public static List<InspectionRecord> Inspections = new();
    public static List<MaintenanceRecord> Maintenances = new();
    public static List<AlarmRecord> Alarms = new();

    public static List<MockEquipData> Equipments = new();
    public static List<MockTechData> Technicians = new();
    public static List<MockManualData> Manuals = new();

    private static long _nextInspectionId = 7;
    private static long _nextMaintenanceId = 7;

    static MonitoringMockDataStore()
    {
        // 1. Seed Equipments
        Equipments.Add(new MockEquipData { id = "1", name = "스마트 크레인 A", model = "CR-100", location = "제1공장" });
        Equipments.Add(new MockEquipData { id = "2", name = "스마트 컨베이어 B", model = "CV-200", location = "제2공장" });
        Equipments.Add(new MockEquipData { id = "3", name = "고속 모터 C", model = "MT-50", location = "제3공장" });
        Equipments.Add(new MockEquipData { id = "4", name = "스마트 펌프 D", model = "PP-300", location = "제1공장" });
        Equipments.Add(new MockEquipData { id = "5", name = "자동화 로봇 E", model = "RB-500", location = "제2공장" });

        // 2. Seed Technicians
        Technicians.Add(new MockTechData { id = "101", name = "김철수", role = "Senior Engineer", department = "유지보수팀" });
        Technicians.Add(new MockTechData { id = "102", name = "이영희", role = "Technician", department = "운영팀" });
        Technicians.Add(new MockTechData { id = "103", name = "박민수", role = "Field Specialist", department = "유지보수팀" });
        Technicians.Add(new MockTechData { id = "104", name = "최다혜", role = "Assistant Technician", department = "원격지원팀" });

        // 3. Seed Manuals
        Manuals.Add(new MockManualData { id = "11", equipmentId = "1", title = "크레인 정기 점검 가이드", category = "REGULAR" });
        Manuals.Add(new MockManualData { id = "12", equipmentId = "1", title = "크레인 비상 조치 매뉴얼", category = "EMERGENCY" });
        Manuals.Add(new MockManualData { id = "13", equipmentId = "1", title = "크레인 와이어 교체 절차서", category = "PARTS_REPLACEMENT" });

        Manuals.Add(new MockManualData { id = "21", equipmentId = "2", title = "컨베이어 정기 윤활 가이드", category = "REGULAR" });
        Manuals.Add(new MockManualData { id = "22", equipmentId = "2", title = "컨베이어 과열 응급 조치", category = "EMERGENCY" });
        Manuals.Add(new MockManualData { id = "23", equipmentId = "2", title = "컨베이어 벨트 교환 매뉴얼", category = "PARTS_REPLACEMENT" });

        Manuals.Add(new MockManualData { id = "31", equipmentId = "3", title = "모터 진동 측정 정기 매뉴얼", category = "REGULAR" });
        Manuals.Add(new MockManualData { id = "32", equipmentId = "3", title = "모터 절연 불량 발생 대처법", category = "EMERGENCY" });
        Manuals.Add(new MockManualData { id = "33", equipmentId = "3", title = "모터 베어링 부품 교체 매뉴얼", category = "PARTS_REPLACEMENT" });

        Manuals.Add(new MockManualData { id = "41", equipmentId = "4", title = "펌프 누설 점검 일지 양식", category = "REGULAR" });
        Manuals.Add(new MockManualData { id = "42", equipmentId = "4", title = "펌프 캐비테이션 대응 지침", category = "EMERGENCY" });
        Manuals.Add(new MockManualData { id = "43", equipmentId = "4", title = "펌프 임펠러 교환서", category = "PARTS_REPLACEMENT" });

        Manuals.Add(new MockManualData { id = "51", equipmentId = "5", title = "로봇 관절 그리스 주입 가이드", category = "REGULAR" });
        Manuals.Add(new MockManualData { id = "52", equipmentId = "5", title = "로봇 충돌 센서 에러 초기화", category = "EMERGENCY" });
        Manuals.Add(new MockManualData { id = "53", equipmentId = "5", title = "로봇 서보 보드 교체서", category = "PARTS_REPLACEMENT" });

        // 4. Seed Inspections (Month: April 2026 to ensure calculations align with dynamic KPI expectations or current dates)
        Inspections.Add(new InspectionRecord { Id = "1", InspectionDate = new DateTime(2026, 4, 10, 10, 0, 0), InspectorName = "김철수", Result = "normal", Note = "정상 작동 확인" });
        Inspections.Add(new InspectionRecord { Id = "2", InspectionDate = new DateTime(2026, 4, 15, 14, 0, 0), InspectorName = "이영희", Result = "issue_found", Note = "센서 오차 범위 초과" });
        Inspections.Add(new InspectionRecord { Id = "3", InspectionDate = new DateTime(2026, 4, 20, 11, 30, 0), InspectorName = "박민수", Result = "normal", Note = "특이사항 없음" });
        Inspections.Add(new InspectionRecord { Id = "4", InspectionDate = new DateTime(2026, 4, 25, 16, 15, 0), InspectorName = "최다혜", Result = "in_progress", Note = "원격 테스트 진행 중" });
        Inspections.Add(new InspectionRecord { Id = "5", InspectionDate = new DateTime(2026, 4, 28, 09, 0, 0), InspectorName = "김철수", Result = "pending", Note = "정기 점검 예정" });
        Inspections.Add(new InspectionRecord { Id = "6", InspectionDate = new DateTime(2026, 4, 29, 13, 0, 0), InspectorName = "박민수", Result = "pending", Note = "모니터링 일정" });

        // 5. Seed Maintenances
        Maintenances.Add(new MaintenanceRecord { Id = "1", Date = new DateTime(2026, 5, 2, 10, 0, 0), ScheduledAt = new DateTime(2026, 5, 2, 10, 0, 0), CompletedAt = new DateTime(2026, 5, 2, 12, 0, 0), EquipmentName = "스마트 크레인 A", Description = "크레인 와이어 장력 조절", ManualTitle = "크레인 정기 점검 가이드", TechnicianName = "김철수", Status = "completed" });
        Maintenances.Add(new MaintenanceRecord { Id = "2", Date = new DateTime(2026, 5, 5, 14, 0, 0), ScheduledAt = new DateTime(2026, 5, 5, 14, 0, 0), CompletedAt = new DateTime(2026, 5, 5, 17, 0, 0), EquipmentName = "스마트 컨베이어 B", Description = "벨트 균열 부분 정비", ManualTitle = "컨베이어 벨트 교환 매뉴얼", TechnicianName = "이영희", Status = "completed" });
        Maintenances.Add(new MaintenanceRecord { Id = "3", Date = new DateTime(2026, 5, 10, 11, 0, 0), ScheduledAt = new DateTime(2026, 5, 10, 11, 0, 0), CompletedAt = null, EquipmentName = "고속 모터 C", Description = "과열 진동 부품 정비", ManualTitle = "모터 베어링 부품 교체 매뉴얼", TechnicianName = "박민수", Status = "in_progress" });
        Maintenances.Add(new MaintenanceRecord { Id = "4", Date = new DateTime(2026, 5, 15, 09, 0, 0), ScheduledAt = new DateTime(2026, 5, 15, 09, 0, 0), CompletedAt = null, EquipmentName = "스마트 펌프 D", Description = "소음 정기 측정 및 점검", ManualTitle = "펌프 누설 점검 일지 양식", TechnicianName = "최다혜", Status = "scheduled" });
        Maintenances.Add(new MaintenanceRecord { Id = "5", Date = new DateTime(2026, 5, 20, 13, 0, 0), ScheduledAt = new DateTime(2026, 5, 20, 13, 0, 0), CompletedAt = null, EquipmentName = "자동화 로봇 E", Description = "그리스 주입 및 필터 청소", ManualTitle = "로봇 관절 그리스 주입 가이드", TechnicianName = "김철수", Status = "scheduled" });
        Maintenances.Add(new MaintenanceRecord { Id = "6", Date = new DateTime(2026, 5, 1, 08, 0, 0), ScheduledAt = new DateTime(2026, 5, 1, 08, 0, 0), CompletedAt = null, EquipmentName = "스마트 크레인 A", Description = "노후 유압 호스 수리 취소", ManualTitle = "크레인 비상 조치 매뉴얼", TechnicianName = "박민수", Status = "cancelled" });

        // 6. Seed Alarms
        Alarms.Add(new AlarmRecord { Id = "1", AlarmCode = "ALM-CR01", TriggeredAt = DateTime.Now.AddDays(-5), EquipmentName = "스마트 크레인 A", Location = "제1공장", Description = "와이어 장력 저하 감지", Severity = "warning", IsActive = true });
        Alarms.Add(new AlarmRecord { Id = "2", AlarmCode = "ALM-CV02", TriggeredAt = DateTime.Now.AddDays(-4), EquipmentName = "스마트 컨베이어 B", Location = "제2공장", Description = "컨베이어 과속 경고", Severity = "warning", IsActive = false });
        Alarms.Add(new AlarmRecord { Id = "3", AlarmCode = "ALM-MT03", TriggeredAt = DateTime.Now.AddHours(-12), EquipmentName = "고속 모터 C", Location = "제3공장", Description = "모터 이상 진동 발생 (치명적)", Severity = "critical", IsActive = true });
        Alarms.Add(new AlarmRecord { Id = "4", AlarmCode = "ALM-PP04", TriggeredAt = DateTime.Now.AddHours(-3), EquipmentName = "스마트 펌프 D", Location = "제1공장", Description = "펌프 유량 이상 변동", Severity = "info", IsActive = true });
        Alarms.Add(new AlarmRecord { Id = "5", AlarmCode = "ALM-RB05", TriggeredAt = DateTime.Now.AddDays(-2), EquipmentName = "자동화 로봇 E", Location = "제2공장", Description = "서보 모터 고온 에러", Severity = "critical", IsActive = false });
        Alarms.Add(new AlarmRecord { Id = "6", AlarmCode = "ALM-CR02", TriggeredAt = DateTime.Now.AddDays(-1), EquipmentName = "스마트 크레인 A", Location = "제1공장", Description = "부하 한계 접근 (정상 범주)", Severity = "info", IsActive = false });
    }

    // --- Helper Methods ---

    public static Task<List<InspectionRecord>> SearchInspections(string keyword)
    {
        var resultList = new List<InspectionRecord>();
        string kw = (keyword ?? "").Trim().ToLowerInvariant();

        foreach (var r in Inspections)
        {
            if (string.IsNullOrEmpty(kw))
            {
                resultList.Add(r);
                continue;
            }

            bool matches = r.Id.ToLowerInvariant().Contains(kw) ||
                           (r.InspectorName ?? "").ToLowerInvariant().Contains(kw) ||
                           (r.Note ?? "").ToLowerInvariant().Contains(kw) ||
                           r.StatusLabel.Contains(kw) ||
                           (r.Result ?? "").ToLowerInvariant().Contains(kw) ||
                           r.DisplayId.ToLowerInvariant().Contains(kw);

            if (matches)
            {
                resultList.Add(r);
            }
        }

        // Sort descending by InspectionDate
        resultList.Sort((a, b) => b.InspectionDate.CompareTo(a.InspectionDate));
        return Task.FromResult(resultList);
    }

    public static Task<bool> AddInspection(string equipId, string techId, string result, DateTime scheduledAt, string note)
    {
        var equip = Equipments.Find(e => e.id == equipId);
        var tech = Technicians.Find(t => t.id == techId);

        string inspectorName = tech != null ? tech.name : "미지정";

        var newRec = new InspectionRecord
        {
            Id = _nextInspectionId++.ToString(),
            InspectionDate = scheduledAt,
            InspectorName = inspectorName,
            Result = string.IsNullOrEmpty(result) ? "pending" : result,
            Note = note
        };

        Inspections.Add(newRec);
        return Task.FromResult(true);
    }

    public static Task<bool> UpdateInspection(string id, string equipId, string techId, string result, string note, DateTime scheduledAt)
    {
        var rec = Inspections.Find(r => r.Id == id);
        if (rec != null)
        {
            var tech = Technicians.Find(t => t.id == techId);
            if (tech != null)
            {
                rec.InspectorName = tech.name;
            }
            rec.Result = string.IsNullOrEmpty(result) ? "pending" : result;
            rec.Note = note;
            rec.InspectionDate = scheduledAt;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public static Task<bool> DeleteInspection(string id)
    {
        int count = Inspections.RemoveAll(r => r.Id == id);
        return Task.FromResult(count > 0);
    }

    public static Task<List<MaintenanceRecord>> SearchMaintenances(string keyword)
    {
        var resultList = new List<MaintenanceRecord>();
        string kw = (keyword ?? "").Trim().ToLowerInvariant();

        foreach (var r in Maintenances)
        {
            if (string.IsNullOrEmpty(kw))
            {
                resultList.Add(r);
                continue;
            }

            bool matches = r.Id.ToLowerInvariant().Contains(kw) ||
                           (r.EquipmentName ?? "").ToLowerInvariant().Contains(kw) ||
                           (r.Description ?? "").ToLowerInvariant().Contains(kw) ||
                           (r.ManualTitle ?? "").ToLowerInvariant().Contains(kw) ||
                           (r.TechnicianName ?? "").ToLowerInvariant().Contains(kw) ||
                           r.StatusLabel.Contains(kw) ||
                           (r.Status ?? "").ToLowerInvariant().Contains(kw) ||
                           r.DisplayId.ToLowerInvariant().Contains(kw);

            if (matches)
            {
                resultList.Add(r);
            }
        }

        // Sort descending by COALESCE(completed_at, scheduled_at)
        resultList.Sort((a, b) => b.Date.CompareTo(a.Date));
        return Task.FromResult(resultList);
    }

    public static Task<bool> AddMaintenance(string equipmentId, string manualId, string workType, string technicianId, string description, string status, DateTime scheduledAt, DateTime? completedAt)
    {
        var equip = Equipments.Find(e => e.id == equipmentId);
        var manual = Manuals.Find(m => m.id == manualId);
        var tech = Technicians.Find(t => t.id == technicianId);

        var newRec = new MaintenanceRecord
        {
            Id = _nextMaintenanceId++.ToString(),
            ScheduledAt = scheduledAt,
            CompletedAt = completedAt,
            Date = completedAt ?? scheduledAt,
            EquipmentName = equip != null ? equip.name : "미정",
            Description = description,
            ManualTitle = manual != null ? manual.title : "미정",
            TechnicianName = tech != null ? tech.name : "미지정",
            Status = string.IsNullOrEmpty(status) ? "scheduled" : status
        };

        Maintenances.Add(newRec);
        return Task.FromResult(true);
    }

    public static Task<bool> UpdateMaintenance(string id, string equipmentId, string manualId, string workType, string technicianId, string status, DateTime scheduledAt, DateTime? completedAt)
    {
        var rec = Maintenances.Find(r => r.Id == id);
        if (rec != null)
        {
            var equip = Equipments.Find(e => e.id == equipmentId);
            var manual = Manuals.Find(m => m.id == manualId);
            var tech = Technicians.Find(t => t.id == technicianId);

            rec.ScheduledAt = scheduledAt;
            rec.CompletedAt = completedAt;
            rec.Date = completedAt ?? scheduledAt;
            if (equip != null) rec.EquipmentName = equip.name;
            if (manual != null) rec.ManualTitle = manual.title;
            if (tech != null) rec.TechnicianName = tech.name;
            rec.Status = string.IsNullOrEmpty(status) ? "scheduled" : status;

            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public static Task<bool> DeleteMaintenance(string id)
    {
        int count = Maintenances.RemoveAll(r => r.Id == id);
        return Task.FromResult(count > 0);
    }

    public static Task<List<AlarmRecord>> SearchAlarms(string keyword)
    {
        var resultList = new List<AlarmRecord>();
        string kw = (keyword ?? "").Trim().ToLowerInvariant();

        foreach (var r in Alarms)
        {
            if (string.IsNullOrEmpty(kw))
            {
                resultList.Add(r);
                continue;
            }

            bool matches = r.Id.ToLowerInvariant().Contains(kw) ||
                           (r.AlarmCode ?? "").ToLowerInvariant().Contains(kw) ||
                           (r.EquipmentName ?? "").ToLowerInvariant().Contains(kw) ||
                           (r.Location ?? "").ToLowerInvariant().Contains(kw) ||
                           (r.Description ?? "").ToLowerInvariant().Contains(kw) ||
                           (r.Severity ?? "").ToLowerInvariant().Contains(kw) ||
                           r.SeverityLabel.Contains(kw) ||
                           r.StatusLabel.Contains(kw);

            if (matches)
            {
                resultList.Add(r);
            }
        }

        // Sort descending by TriggeredAt
        resultList.Sort((a, b) => b.TriggeredAt.CompareTo(a.TriggeredAt));
        return Task.FromResult(resultList);
    }
}
