namespace STM32CubeMXCPPConvertor;

public static class FileProcessor {
	public static readonly FileInfo MainCMakeListsFileInfo = new("CMakeLists.txt");
	public static readonly FileInfo CubeMXCMakeListsFileInfo = new("cmake/stm32cubemx/CMakeLists.txt");
	public static readonly FileInfo CubeMXCMakeListsTempFileInfo = new("cmake/stm32cubemx/CMakeLists.txt.tmp");

	private static readonly Version MinimumSupportedCubeMXVersion = new(6, 15, 0);
	private static readonly Version MaximumTestedCubeMXVersion = new(6, 15, 0);
	private static readonly string[] CPPExtensions = [".cpp", ".cxx", ".cc"];

	public static bool IsProcessorInitialized { get; private set; } = false;
	public static string ProjectName { get; private set; } = Path.GetFileName(Directory.GetCurrentDirectory());
	public static FileInfo CubeMXIOCFileInfo { get; private set; } = new($"{ProjectName}.ioc");
	public static Version? CubeMXVersion { get; private set; } = null;

	public static string[] InitializeProcessor() {
		if (IsProcessorInitialized) {
			return [];
		}

		List<string> result = [];

		// 检查主 CMakeLists.txt 文件是否存在
		if (!MainCMakeListsFileInfo.Exists) {
			throw new FileNotFoundException("Main CMakeLists.txt file not found, please generate code firstly.", MainCMakeListsFileInfo.FullName);
		}

		// 从主 CMakeLists.txt 文件中获取项目名称
		UpdateProjectName(result);

		// 检查 CubeMX IOC 文件是否存在
		if (!CubeMXIOCFileInfo.Exists) {
			throw new FileNotFoundException("CubeMX IOC file not found.", CubeMXIOCFileInfo.FullName);
		}

		// 检查 IOC 文件中的项目名称和工具链设置以及 CubeMX 版本
		EnsureCubeMXIOCFile(result);

		// 检查 CubeMX CMakeLists.txt 文件是否存在
		if (!CubeMXCMakeListsFileInfo.Exists) {
			throw new FileNotFoundException("CubeMX CMakeLists.txt file not found.", CubeMXCMakeListsFileInfo.FullName);
		}

		result.Add($"Processor initialized successfully for project '{ProjectName}'.");

		IsProcessorInitialized = true;

		return [..result];
	}

	private static void UpdateProjectName(List<string> result) {
		// 从主 CMakeLists.txt 文件中获取项目名称
		// 获取这一行：set(CMAKE_PROJECT_NAME RM_Dart_NavLight_Driver_2026)
		using var reader = MainCMakeListsFileInfo.OpenText();
		string? line = null;

		var projectNameFound = false;

		while ((line = reader.ReadLine()) != null) {
			if (line.StartsWith("set(CMAKE_PROJECT_NAME ")) {
				// 提取项目名称
				var startIndex = line.IndexOf(' ') + 1;
				var endIndex = line.IndexOf(')', startIndex);
				if (startIndex >= 0 && endIndex > startIndex) {
					ProjectName = line[startIndex..endIndex].Trim();
					CubeMXIOCFileInfo = new($"{ProjectName}.ioc");
					projectNameFound = true;
					break;
				}
			}
		}

		// 如果没有找到项目名称，则显示警告提醒新版本 CubeMX 生成的文件可能已经更新
		if (!projectNameFound) {
			result.Add("Warning: CMAKE_PROJECT_NAME not found in CMakeLists.txt, " +
				"please check if the file was created by a newer version of CubeMX. " +
				"Using default project name: " + ProjectName);
		}
	}

	private static void EnsureCubeMXIOCFile(List<string> result) {
		// 检查 IOC 文件中的项目名称和工具链设置以及 CubeMX 版本
		// 检查这一行：ProjectManager.ProjectName={ProjectName}
		// 检查这一行：ProjectManager.TargetToolchain=CMake
		// 检查这一行：MxCube.Version={Version}
		using var reader = CubeMXIOCFileInfo.OpenText();
		string? line = null;

		var projectNameConfirmed = false;
		var toolchainConfirmed = false;
		var cubeMXVersionConfirmed = false;

		while ((line = reader.ReadLine()) != null) {
			var equalIndex = line.IndexOf('=');
			if (equalIndex < 0) {
				continue; // 没有找到等号，跳过这一行
			}

			var key = line[..equalIndex].TrimEnd();

			if (!projectNameConfirmed && key == "ProjectManager.ProjectName") {
				var value = ParseIocValue(line, equalIndex);

				if (value != ProjectName) {
					throw new InvalidOperationException($"Project name mismatch in IOC file: expected '{ProjectName}', found '{value}'.");
				}

				projectNameConfirmed = true;
				continue;
			}

			if (!toolchainConfirmed && key == "ProjectManager.TargetToolchain") {
				var value = ParseIocValue(line, equalIndex);

				if (value != "CMake") {
					throw new InvalidOperationException($"Target toolchain mismatch in IOC file: expected 'CMake', found '{value}'.");
				}

				toolchainConfirmed = true;
				continue;
			}

			if (!cubeMXVersionConfirmed && key == "MxCube.Version") {
				var value = ParseIocValue(line, equalIndex);

				// 检查 CubeMX 版本是否符合要求
				if (Version.TryParse(value, out var cubeMXVersion)) {
					CubeMXVersion = cubeMXVersion;

					// 低于最低支持版本则抛出异常
					if (CubeMXVersion < MinimumSupportedCubeMXVersion) {
						throw new InvalidOperationException($"CubeMX version {CubeMXVersion} is not supported. Minimum required version is {MinimumSupportedCubeMXVersion}.");
					}

					// 如果 CubeMX 版本高于最大测试通过版本，则添加警告
					if (CubeMXVersion > MaximumTestedCubeMXVersion) {
						result.Add($"Warning: CubeMX version {CubeMXVersion} is newer than the maximum tested version {MinimumSupportedCubeMXVersion}. " +
							"Ensure compatibility with the convertor.");
					}

					cubeMXVersionConfirmed = true;
					continue;
				} else {
					throw new InvalidOperationException($"Invalid CubeMX version format: '{value}'.");
				}
			}

			// 如果三个条件都满足，则可以提前结束读取
			if (projectNameConfirmed && toolchainConfirmed && cubeMXVersionConfirmed) {
				break;
			}
		}

		if (!projectNameConfirmed) {
			throw new InvalidOperationException("Project name not found in IOC file.");
		}

		if (!toolchainConfirmed) {
			throw new InvalidOperationException("Target toolchain not found in IOC file.");
		}

		if (!cubeMXVersionConfirmed) {
			throw new InvalidOperationException("CubeMX version not found in IOC file.");
		}
	}

	private static string ParseIocValue(string line, int equalIndex) =>
		line[(equalIndex + 1)..].TrimStart().Replace(@"\ ", " ").Replace(@"\=", "=").Replace(@"\:", ":").Replace(@"\\", @"\");

	private static bool IsCPPFileMatch(string filePathOfC, out string filePathOfCPP) {
		foreach (var extension in CPPExtensions) {
			filePathOfCPP = Path.ChangeExtension(filePathOfC, extension);
			if (File.Exists(filePathOfCPP)) {
				return true;
			}
		}

		filePathOfCPP = string.Empty;
		return false;
	}

	public static bool TryUpdateToCPPFile(ref string filePath, out string? warning) {
		warning = null;

		if (IsCPPFileMatch(filePath, out var filePathOfCPP)) {
			try {
				// 覆盖移动 .c 文件
				File.Move(filePath, filePath + ".bak", true);
			} catch {
				warning = $"Warning: Failed to backup the original C file '{filePath}'. " +
					"Please ensure you have write permissions or manually backup the file.";
			}

			// 修改文件路径为 C++ 文件
			filePath = filePathOfCPP;
			return true;
		}
		return false;
	}
}