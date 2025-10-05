# Claims Classification Framework

## Overview

The Alchemist Claims Classification system provides intelligent categorization of experiment results, differentiating between regressions, expected anomalies, unexpected anomalies, improvements, and new phenomena. This framework enables automated quality assessment and appropriate follow-up actions for each type of experimental outcome.

## Classification Categories

### Primary Classifications

#### 1. **Validated** (`validated`)
- **Criteria**: Confidence ≥ 75%, successful execution, high-quality metadata
- **Description**: High-confidence positive results that can guide development decisions
- **Directory**: `assets/experiments/school/claims/validated/`
- **Action**: Proceed with implementation based on validated findings

#### 2. **Hypothesis** (`hypothesis`)  
- **Criteria**: Confidence 50-74%, moderate evidence quality
- **Description**: Medium-confidence findings that warrant further investigation
- **Directory**: `assets/experiments/school/claims/hypotheses/`
- **Action**: Conduct additional experiments to increase confidence

#### 3. **Regression** (`regression`)
- **Criteria**: Confidence < 50%, failed execution, negative performance indicators
- **Description**: Performance degradation or negative trends requiring attention
- **Directory**: `assets/experiments/school/claims/regressions/`
- **Action**: Investigate root cause and implement fixes

#### 4. **Anomaly** (`anomaly`)
- **Criteria**: Anomaly Score ≥ 0.3, unusual patterns detected
- **Description**: Unexpected patterns that deviate from expected behavior
- **Directory**: `assets/experiments/school/claims/anomalies/`
- **Subtypes**:
  - **Expected Anomalies**: Anticipated deviations (stress tests, experimental branches)
  - **Unexpected Anomalies**: Truly surprising patterns requiring investigation
- **Action**: Analysis to determine if expected or concerning

#### 5. **Improvement** (`improvement`)
- **Criteria**: Improvement Score ≥ 0.8, successful execution, positive indicators
- **Description**: Significant positive changes representing measurable enhancements
- **Directory**: `assets/experiments/school/claims/improvements/`
- **Action**: Document patterns and consider applying similar optimizations

#### 6. **New Phenomenon** (`new_phenomenon`)
- **Criteria**: Phenomenon Score ≥ 0.5, novel patterns detected
- **Description**: Previously unobserved behaviors potentially representing breakthroughs
- **Directory**: `assets/experiments/school/claims/new_phenomena/`
- **Action**: Deep investigation and documentation of novel discovery

## Classification Algorithm

### Step 1: Base Classification
```
Confidence ≥ 75% → Validated
Confidence 50-74% → Hypothesis  
Confidence < 50% → Regression
```

### Step 2: Anomaly Detection
Anomaly patterns are detected by analyzing:

- **Execution Time Anomalies**:
  - Very fast execution (< 2 seconds): +0.3 anomaly score
  - Very slow execution (> 15 minutes): +0.4 anomaly score

- **Expected Anomalies** (flagged as expected):
  - Experiment names containing: "stress", "load", "boundary", "edge"
  - Git branches containing: "experimental", "prototype", "research", "spike"

- **Behavioral Anomalies**:
  - Success with very low confidence (< 0.3): +0.5 anomaly score
  - Failure with high confidence (> 0.7): +0.4 anomaly score

### Step 3: Improvement Detection
Improvement patterns identified by:

- **High Confidence Success**: Confidence ≥ 0.8 with successful execution
- **Performance Indicators**: Experiment names containing "optimization", "performance", "efficiency", "speed"
- **User Experience Indicators**: Experiment names containing "ui", "ux", "usability", "experience"

### Step 4: New Phenomenon Detection
Novel patterns detected through:

- **Innovation Indicators**: Experiment names containing "novel", "new", "innovative", "breakthrough"
- **Exceptional Confidence**: Confidence > 0.9 with successful execution
- **Exploratory Context**: Git branches containing "discovery", "exploration", "investigation"

### Step 5: Domain-Specific Rules

#### Performance Experiments
- Require higher confidence (≥ 0.8) for validation
- Lower tolerance for execution anomalies

#### UI/UX Experiments
- Slightly lower validation threshold (≥ 0.7)
- More subjective, qualitative assessment

#### Integration Experiments
- Binary success/failure assessment
- Successful integration → validated (confidence ≥ 0.6)
- Failed integration → regression

#### Security Experiments
- Very high confidence required (≥ 0.85) for validation
- Conservative approach to ensure safety

#### Data Processing Experiments
- Confidence threshold ≥ 0.75 for validation
- Careful assessment for data quality biases

## Classification Metadata

Each claim includes detailed classification metadata:

```json
{
  "Classification": {
    "PrimaryType": "anomaly",
    "SecondaryType": "expected",
    "AnomalyScore": 0.45,
    "ClassificationFlags": [
      "expected_stress_test",
      "execution_too_slow",
      "high_confidence"
    ],
    "ClassificationReason": "Expected anomaly detected (score: 0.45)",
    "TrendSignificance": 0.78,
    "BaselineDeviation": "pending_baseline_analysis",
    "IsExpectedAnomaly": true,
    "PhenomenonType": ""
  }
}
```

### Classification Flags

Common flags applied during classification:

#### Confidence Flags
- `very_high_confidence` (≥ 0.9)
- `high_confidence` (≥ 0.75)
- `moderate_confidence` (≥ 0.5)
- `low_confidence` (< 0.5)

#### Execution Flags
- `successful_execution`
- `failed_execution`
- `execution_too_fast`
- `execution_too_slow`

#### Context Flags
- `expected_stress_test`
- `experimental_branch`
- `success_confidence_mismatch`
- `failure_confidence_mismatch`
- `performance_optimization`
- `user_experience_enhancement`
- `novel_experiment_indicator`

## Edge Cases and Borderline Claims

### Ambiguous Classifications
Claims that fall into borderline categories are handled through:

1. **Threshold Overlap Zones**: 
   - Confidence 74-76%: Additional metadata analysis
   - Anomaly scores 0.25-0.35: Context-dependent classification

2. **Multi-Factor Resolution**:
   - Git context quality assessment
   - Hypothesis quality evaluation
   - Domain-specific rule application

3. **Conservative Defaults**:
   - When uncertain, err on the side of requiring more validation
   - Default to "hypothesis" category for borderline cases

### Confidence Score Adjustments

Base confidence can be modified by:

- **Execution Consistency**: ±20% based on error log analysis
- **Metadata Completeness**: -5% to -10% for missing metadata
- **Git Context Quality**: ±5% based on branch hygiene and commit practices

## Integration with Regression Tracking

The classification system integrates with the existing `RegressionTracker` for enhanced analysis:

### Severity Mapping
```
RegressionSeverity.CRITICAL (>50%) → regression (high priority)
RegressionSeverity.MAJOR (30-50%) → regression (medium priority)  
RegressionSeverity.MODERATE (15-30%) → anomaly (if unexpected)
RegressionSeverity.MINOR (5-15%) → hypothesis (needs validation)
```

### Trend Analysis
- **TrendDirection.DEGRADING** → Increases regression likelihood
- **TrendDirection.IMPROVING** → Increases improvement likelihood
- **TrendDirection.VOLATILE** → Increases anomaly likelihood

## Validation and Testing

### Test Scenarios
1. **High-confidence successful experiments** → Should classify as validated
2. **Boundary condition tests** → Should classify as expected anomalies
3. **Failed experiments with good metadata** → Should classify as regressions
4. **Novel experimental patterns** → Should classify as new phenomena
5. **Performance optimization experiments** → Should classify as improvements

### Edge Case Testing
- Confidence exactly at thresholds (50%, 75%)
- Missing or incomplete metadata
- Conflicting success/confidence patterns
- Unusual execution times
- Mixed positive/negative indicators

## Future Enhancements

### Machine Learning Integration
- Pattern recognition for anomaly detection
- Historical trend analysis for improvement classification
- Automated threshold tuning based on outcome validation

### Cross-Experiment Analysis
- Multi-experiment phenomenon detection
- Trend correlation across experiment types
- Baseline drift detection and adjustment

### Advanced Reporting
- Classification trend analysis over time
- Domain-specific classification performance metrics
- Automated follow-up action recommendations