#!/bin/bash

# GcpvLynx Test Runner
# ====================

echo "🧪 GcpvLynx Test Suite"
echo "======================"

# Check if .NET is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET is not installed or not in PATH"
    exit 1
fi

# Display .NET version
echo "✅ .NET version: $(dotnet --version)"

# Check if test runner exists
TEST_RUNNER_DIR="../TestRunner"
if [ ! -d "$TEST_RUNNER_DIR" ]; then
    echo "❌ Test runner directory not found: $TEST_RUNNER_DIR"
    exit 1
fi

# Check if test runner project exists
if [ ! -f "$TEST_RUNNER_DIR/TestRunner.csproj" ]; then
    echo "❌ Test runner project not found: $TEST_RUNNER_DIR/TestRunner.csproj"
    exit 1
fi

echo "🔨 Building test runner..."
cd "$TEST_RUNNER_DIR"

# Build the test runner
if ! dotnet build --verbosity quiet; then
    echo "❌ Failed to build test runner"
    exit 1
fi

echo "✅ Build successful!"
echo ""

# Run the tests
echo "🚀 Running tests..."
echo "==================="
echo ""

if dotnet run; then
    echo ""
    echo "✅ All tests completed successfully!"
    exit 0
else
    echo ""
    echo "❌ Some tests failed!"
    exit 1
fi
