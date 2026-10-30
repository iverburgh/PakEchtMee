# CLAUDE.md

De project-instructies worden gedeeld met GitHub Copilot en staan op één plek:

@.github/copilot-instructions.md

## OpenSpec workflow

Dit project gebruikt [OpenSpec](https://github.com/Fission-AI/OpenSpec) (schema `spec-driven`, zie `openspec/config.yaml`).

- Actuele specs: `openspec/specs/<capability>/spec.md`
- Lopende changes: `openspec/changes/<change>/` (proposal, design, specs-delta, tasks)
- Gearchiveerde changes: `openspec/changes/archive/`

Slash commands (`.claude/commands/opsx/`) en skills (`.claude/skills/openspec-*`):

| Command          | Doel                                                        |
| ---------------- | ----------------------------------------------------------- |
| `/opsx:explore`  | Meedenken/verkennen zonder code te wijzigen                 |
| `/opsx:propose`  | Nieuwe change met proposal, design, specs en tasks          |
| `/opsx:update`   | Artifacts van een bestaande change bijwerken                |
| `/opsx:apply`    | Tasks van een change implementeren                          |
| `/opsx:sync`     | Delta-specs van een change naar `openspec/specs/` syncen    |
| `/opsx:archive`  | Afgeronde change archiveren                                 |

De `openspec` CLI moet geïnstalleerd zijn (`npm i -g @fission-ai/openspec`).
Na een CLI-upgrade: `openspec update` ververst zowel de Claude- als de Copilot-bestanden.

## Skills

- `.claude/skills/aspire` is een kopie van `.agents/skills/aspire` (Copilot). Houd ze gelijk bij wijzigingen.
