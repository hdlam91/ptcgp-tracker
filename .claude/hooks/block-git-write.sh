#!/bin/bash

INPUT=$(cat)

COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // ""')

if echo "$COMMAND" | grep -Eq '(^|[;&|[:space:]])git[[:space:]]+(commit|push)([[:space:]]|$)'; then
    echo "BLOCKED: Claude is not allowed to commit or push. The user handles commits and pushes manually." >&2
    exit 2
fi

exit 0