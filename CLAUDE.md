# Global Agent Instructions

IMPORTANT: Claude, you are strictly required to scan, read, and follow the localized project configurations hidden in the subfolders. 

Before processing any user request, you MUST automatically call your file tools to read:
- `Rule/.claudeignore`
- `Rule/RULE.md`
- `Rule/MEM.md`
- `Rule/color.md`
- `Rule/code.md`
- `Rule/DOMAIN.md`
- `README.md`

Execute this auto-discovery workflow immediately at the start of every session to ensure you adhere to our N-Tier Architecture, Git Branching rules, and Commit Conventions.