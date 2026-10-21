#!/usr/bin/env python3
"""The grants of an approved /minigame plan, run by the USER (not the agent) at the end of Phase 1.

Usage: python3 .claude/skills/minigame/scripts/grant.py [--revoke]

- .claude/settings.local.json: allow `git add`, `git commit`, the build-tiktok scripts and the
  minigame scripts; set env.UNITY_PROJECT_PATH to this checkout (several Editors may run at once).
- Library/Toolkit/unlock: ProjectSettings/ and Assets/SDKSetting/, the two protected paths the
  sdk and build stages change.
What was added is recorded in Library/Toolkit/minigame/grants.json; --revoke removes exactly that,
never a rule or an unlock line that was there before. Both files are per machine and not committed.
"""
import json
import os
import sys

ALLOW = [
    "Bash(git add *)",
    "Bash(git commit *)",
    "Bash(.claude/skills/build-tiktok/scripts/*)",
    "Bash(python3 .claude/skills/minigame/scripts/*)",
]
UNLOCK = ["ProjectSettings/", "Assets/SDKSetting/"]


def repo_root():
    here = os.path.dirname(os.path.abspath(__file__))
    return os.path.abspath(os.path.join(here, "..", "..", "..", ".."))


def read_json(path, fallback):
    if not os.path.exists(path):
        return fallback
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def write_json(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")


def read_lines(path):
    if not os.path.exists(path):
        return []
    with open(path, encoding="utf-8") as f:
        return [line.rstrip("\n") for line in f if line.strip()]


def write_lines(path, lines):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write("".join(line + "\n" for line in lines))


def main():
    revoke = "--revoke" in sys.argv[1:]
    root = repo_root()
    settings_path = os.path.join(root, ".claude", "settings.local.json")
    unlock_path = os.path.join(root, "Library", "Toolkit", "unlock")
    record_path = os.path.join(root, "Library", "Toolkit", "minigame", "grants.json")

    settings = read_json(settings_path, {})
    allow = settings.setdefault("permissions", {}).setdefault("allow", [])
    env = settings.setdefault("env", {})
    unlock = read_lines(unlock_path)
    record = read_json(record_path, {"allow": [], "unlock": [], "env": False})

    if revoke:
        allow[:] = [rule for rule in allow if rule not in record["allow"]]
        unlock = [line for line in unlock if line not in record["unlock"]]
        if record["env"]:
            env.pop("UNITY_PROJECT_PATH", None)
        record = {"allow": [], "unlock": [], "env": False}
    else:
        for rule in ALLOW:
            if rule not in allow:
                allow.append(rule)
                record["allow"].append(rule)
        for prefix in UNLOCK:
            if prefix not in unlock:
                unlock.append(prefix)
                record["unlock"].append(prefix)
        if env.get("UNITY_PROJECT_PATH") != root:
            record["env"] = record["env"] or "UNITY_PROJECT_PATH" not in env
            env["UNITY_PROJECT_PATH"] = root

    if not env:
        settings.pop("env")
    write_json(settings_path, settings)
    write_lines(unlock_path, unlock)
    write_json(record_path, record)

    if revoke:
        print("revoked what grant.py had added")
    else:
        print(f"granted: {', '.join(ALLOW)}; env UNITY_PROJECT_PATH={root}; unlock {', '.join(UNLOCK)}")
        print("Reply 'go' in the Claude session. UNITY_PROJECT_PATH applies from the next session; "
              "until then the run passes --project-path itself.")


if __name__ == "__main__":
    main()
