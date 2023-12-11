SELECT 
    d.DeviceID, 
    d.Model, 
    d.ManufactureDate, 
    d.WarrantyPeriod, 
    d.Description AS DeviceDescription,
    GROUP_CONCAT(DISTINCT m.Type ORDER BY m.ModuleID SEPARATOR ', ') AS ModuleTypes,
    GROUP_CONCAT(DISTINCT p.Name ORDER BY p.PartID SEPARATOR ', ') AS PartNames,
    COUNT(DISTINCT m.ModuleID) AS NumberOfModules,
    COUNT(DISTINCT p.PartID) AS NumberOfParts
FROM 
    Devices d
LEFT JOIN 
    DeviceModules dm ON d.DeviceID = dm.DeviceID
LEFT JOIN 
    Modules m ON dm.ModuleID = m.ModuleID
LEFT JOIN 
    ModuleParts mp ON m.ModuleID = mp.ModuleID
LEFT JOIN 
    Parts p ON mp.PartID = p.PartID
WHERE 
    d.DeviceID = [Your_Device_ID]
GROUP BY 
    d.DeviceID;


SELECT 
    d.DeviceID, 
    d.Model, 
    d.ManufactureDate, 
    d.WarrantyPeriod, 
    d.Description AS DeviceDescription,
    m.ModuleID, 
    m.Type AS ModuleType, 
    m.Description AS ModuleDescription,
    p.PartID, 
    p.Name AS PartName, 
    p.Type AS PartType, 
    p.Specification,
    dm.Quantity AS ModuleQuantity,
    mp.Quantity AS PartQuantity
FROM 
    Devices d
JOIN 
    DeviceModules dm ON d.DeviceID = dm.DeviceID
JOIN 
    Modules m ON dm.ModuleID = m.ModuleID
JOIN 
    ModuleParts mp ON m.ModuleID = mp.ModuleID
JOIN 
    Parts p ON mp.PartID = p.PartID
WHERE 
    d.DeviceID = [Your_Device_ID];


