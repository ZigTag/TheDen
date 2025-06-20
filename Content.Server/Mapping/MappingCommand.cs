// SPDX-FileCopyrightText: 2021 AJCM-git <60196617+AJCM-git@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2022 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 Kevin Zheng <kevinz5000@gmail.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Debug <49997488+DebugOk@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 DEATHB4DEFEAT <77995199+DEATHB4DEFEAT@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 Falcon <falcon@zigtag.dev>
// SPDX-FileCopyrightText: 2025 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <flyingkarii@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Mapping
{
    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    sealed class MappingCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public string Command => "mapping";
        public string Description => Loc.GetString("cmd-mapping-desc");
        public string Help => Loc.GetString("cmd-mapping-help");

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-mapping-id"));
                case 2:
                    var res = IoCManager.Resolve<IResourceManager>();
                    var opts = CompletionHelper.UserFilePath(args[1], res.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[1], res));
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-mapping-path"));
            }
            return CompletionResult.Empty;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("cmd-savemap-server"));
                return;
            }

            if (args.Length > 2)
            {
                shell.WriteLine(Help);
                return;
            }

#if DEBUG
            shell.WriteLine(Loc.GetString("cmd-mapping-warning"));
#endif

            MapId mapId;
            string? toLoad = null;
            var mapSys = _entities.System<SharedMapSystem>();

            // Get the map ID to use
            if (args.Length is 1 or 2)
            {
                if (!int.TryParse(args[0], out var intMapId))
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-failure-integer", ("arg", args[0])));
                    return;
                }

                mapId = new MapId(intMapId);

                // no loading null space
                if (mapId == MapId.Nullspace)
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-nullspace"));
                    return;
                }

                if (_map.MapExists(mapId))
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-exists", ("mapId", mapId)));
                    return;
                }

                // either load a map or create a new one.
                if (args.Length <= 1)
                {
                    mapSys.CreateMap(mapId, runMapInit: false);
                }
                else
                {
                    var path = new ResPath(args[1]);
                    var opts = new DeserializationOptions {StoreYamlUids = true};
                    _entities.System<MapLoaderSystem>().TryLoadMapWithId(mapId, path, out _, out _, opts);
                }

                // was the map actually created or did it fail somehow?
                if (!_map.MapExists(mapId))
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-error"));
                    return;
                }
            }
            else
            {
                mapSys.CreateMap(out mapId, runMapInit: false);
            }

            // map successfully created. run misc helpful mapping commands
            if (player.AttachedEntity is { Valid: true } playerEntity &&
                _entities.GetComponent<MetaDataComponent>(playerEntity).EntityPrototype?.ID != GameTicker.AdminObserverPrototypeName)
            {
                shell.ExecuteCommand("aghost");
            }

            // don't interrupt mapping with events or auto-shuttle
            shell.ExecuteCommand("sudo cvar events.enabled false");
            shell.ExecuteCommand("sudo cvar shuttle.auto_call_time 0");

            if (_cfg.GetCVar(CCVars.AutosaveEnabled))
                shell.ExecuteCommand($"toggleautosave {mapId} {toLoad ?? "NEWMAP"}");
            shell.ExecuteCommand($"tp 0 0 {mapId}");
            shell.RemoteExecuteCommand("mappingclientsidesetup");
            _map.SetMapPaused(mapId, true);

            if (args.Length == 2)
                shell.WriteLine(Loc.GetString("cmd-mapping-success-load",("mapId",mapId),("path", args[1])));
            else
                shell.WriteLine(Loc.GetString("cmd-mapping-success", ("mapId", mapId)));
        }
    }
}
