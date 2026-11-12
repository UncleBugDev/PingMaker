using System;
using PingMaker.Configuration;
using UnityEngine;

namespace PingMaker.Network
{
    public struct PingPackageData
    {
        public Vector3 WorldPosition;
        public ZDOID TargetCreatureZDOID;
        public int PingType; // Represents PingCategory (0 = World, 1 = Creature)
        public Color PingColor;
        public string SenderName;
        public float Duration;
        public string TargetName;
        public int TargetLevel;

        public PingCategory Category => (PingCategory)PingType;

        public PingPackageData(
            Vector3 worldPosition,
            ZDOID targetCreatureZDOID,
            PingCategory category,
            Color pingColor,
            string senderName,
            float duration,
            string targetName = "",
            int targetLevel = 1)
        {
            WorldPosition = worldPosition;
            TargetCreatureZDOID = targetCreatureZDOID;
            PingType = (int)category;
            PingColor = pingColor;
            SenderName = senderName ?? "Viking";
            Duration = duration > 0f ? duration : 20f;
            TargetName = targetName ?? "";
            TargetLevel = targetLevel;
        }

        public ZPackage ToZPackage()
        {
            ZPackage pkg = new ZPackage();

            // 1. World Position
            pkg.Write(WorldPosition);

            // 2. Creature ZDOID (or ZDOID.None)
            pkg.Write(TargetCreatureZDOID);

            // 3. Ping Type (Category)
            pkg.Write(PingType);

            // 4. Color RGBA
            pkg.Write(PingColor.r);
            pkg.Write(PingColor.g);
            pkg.Write(PingColor.b);
            pkg.Write(PingColor.a);

            // 5. Sender Name
            pkg.Write(SenderName ?? "");

            // 6. Duration
            pkg.Write(Duration);

            // 7. Optional Target Name & Level
            pkg.Write(TargetName ?? "");
            pkg.Write(TargetLevel);

            return pkg;
        }

        public static bool TryDeserialize(ZPackage pkg, out PingPackageData data)
        {
            data = default;
            if (pkg == null) return false;

            try
            {
                data.WorldPosition = pkg.ReadVector3();
                data.TargetCreatureZDOID = pkg.ReadZDOID();
                data.PingType = pkg.ReadInt();

                float r = pkg.ReadSingle();
                float g = pkg.ReadSingle();
                float b = pkg.ReadSingle();
                float a = pkg.ReadSingle();
                data.PingColor = new Color(r, g, b, a);

                data.SenderName = pkg.ReadString();
                data.Duration = pkg.ReadSingle();

                // Safe optional reads
                if (pkg.Size() > pkg.GetPos())
                {
                    data.TargetName = pkg.ReadString();
                }
                else
                {
                    data.TargetName = "";
                }

                if (pkg.Size() > pkg.GetPos())
                {
                    data.TargetLevel = pkg.ReadInt();
                }
                else
                {
                    data.TargetLevel = 1;
                }

                // Sanity check coordinates
                if (float.IsNaN(data.WorldPosition.x) || float.IsInfinity(data.WorldPosition.x))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                PingMakerPlugin.Log.LogError($"[PingMaker] Ошибка десериализации PingPackage: {ex.Message}");
                return false;
            }
        }
    }
}
