# Contributing

Thanks for helping improve Dreamine.MVVM.FullKit.

This repository is a FullKit coordination repository. Most library code lives in Git submodules under `20_SOURCES/100. Library`. A change may therefore require commits in one or more child repositories first, followed by a parent repository commit that updates the submodule pointers.

## Setup

Clone with submodules:

```powershell
git clone --recursive https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.FullKit.git
cd Dreamine.MVVM.FullKit
```

If the repository was cloned without submodules:

```powershell
git submodule update --init --recursive
```

## Before Opening A PR

Run the same core checks used by CI:

```powershell
dotnet test "20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Dreamine.FullKit.Tests.csproj" --no-restore --configuration Release -m:1
dotnet test "20_SOURCES/200. Tests/Dreamine.FullKit.Wpf.Tests/Dreamine.FullKit.Wpf.Tests.csproj" --no-restore --configuration Release -m:1
dotnet test "20_SOURCES/200. Tests/UI.WinForms.Tests/Dreamine.UI.WinForms.Tests.csproj" --no-restore --configuration Release -m:1
```

For UI-facing changes, also build representative samples:

```powershell
dotnet build "20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleCore/SampleCore.csproj" --no-restore --configuration Release -m:1
dotnet build "20_SOURCES/998. DEMO/000. Sample/050. CrossUi/SampleCrossUi.Wpf/SampleCrossUi.Wpf.csproj" --no-restore --configuration Release -m:1
dotnet build "20_SOURCES/000. Project/010. App/Dreamine.Web/Dreamine.Web.csproj" --no-restore --configuration Release -m:1
```

## Submodule Changes

When modifying a submodule:

1. Commit and push the submodule repository first.
2. Return to the FullKit repository.
3. Stage the changed submodule directory so the parent repository records the new commit pointer.
4. Commit and push the FullKit parent repository.

Do not commit generated `bin` or `obj` directories. Keep unrelated submodule pointer changes out of the same PR when possible.

## Style

- Prefer the existing project layout and naming conventions.
- Keep comments short and useful.
- Keep public XML docs accurate for APIs that are part of library packages.
- For WPF/XAML work, verify both build output and Visual Studio IntelliSense warnings where practical.
