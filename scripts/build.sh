#!/usr/bin/env bash
set -euo pipefail

# --------------------------------------------
# Ensure dotnet is installed
# --------------------------------------------
if ! command -v dotnet &>/dev/null; then
    echo "dotnet not found. Installing the latest stable SDK..."

    # Detect OS
    OS=$(uname -s | tr '[:upper:]' '[:lower:]')
    case "$OS" in
        linux*)
            # Linux: use install script
            ;;
        darwin*)
            # macOS
            ;;
        *)
            echo "Unsupported OS: $OS. Please install dotnet manually."
            exit 1
            ;;
    esac

    # Download and run the official install script
    INSTALL_SCRIPT="/tmp/dotnet-install.sh"
    curl -sSL https://dot.net/v1/dotnet-install.sh -o "$INSTALL_SCRIPT"
    chmod +x "$INSTALL_SCRIPT"

    DOTNET_INSTALL_DIR="${HOME}/.dotnet"
    "$INSTALL_SCRIPT" --install-dir "$DOTNET_INSTALL_DIR" --channel LTS --no-path

    # Add to PATH for this session
    export PATH="$DOTNET_INSTALL_DIR:$PATH"

    # Verify installation
    if ! command -v dotnet &>/dev/null; then
        echo "Failed to install dotnet. Please install it manually."
        exit 1
    fi
    echo "dotnet installed successfully: $(dotnet --version)"
else
    echo "dotnet found: $(dotnet --version)"
fi

# --------------------------------------------
# Main script logic
# --------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(dirname "$SCRIPT_DIR")"
PACKAGE_OUTPUT="${REPOSITORY_ROOT}/.artifacts"

mkdir -p "$PACKAGE_OUTPUT"

package_projects=(
    "src/Majal/Majal.csproj"
    "src/Majal.DataTransferObjects/Majal.DataTransferObjects.csproj"
    "src/Majal.EntityFrameworkCore/Majal.EntityFrameworkCore.csproj"
)

sample_projects=(
    "samples/EShop/EShop.csproj"
)

for project in "${package_projects[@]}"; do
    dotnet pack "${REPOSITORY_ROOT}/${project}" \
        -c Release \
        -o "$PACKAGE_OUTPUT" \
        --nologo
done

# NOTE: NuGet's global-packages cache treats a given package id+version as immutable once extracted, so
# repacking the same $(PackageVersion) during local iteration won't be picked up by --force/--no-cache on
# restore below (those flags only bypass the HTTP/remote-feed cache, not ~/.nuget/packages). If a sample
# build seems to be using a stale generator, clear that cache manually, e.g.:
#   dotnet nuget locals global-packages --clear
# (clearing it from within this script was tried and made the very next restore fail in stranger ways than
# the staleness it was meant to fix, so it's left as a manual step instead.)

for project in "${sample_projects[@]}"; do
    sample_project="${REPOSITORY_ROOT}/${project}"
    dotnet restore "$sample_project" \
        --source "$PACKAGE_OUTPUT" \
        --source 'https://api.nuget.org/v3/index.json' \
        --force \
        --no-cache \
        --nologo
    dotnet build "$sample_project" \
        -c Release \
        --no-restore \
        --nologo
done