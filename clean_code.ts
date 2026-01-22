const MIN_REASON_LENGTH = 10;
const MAX_REASON_LENGTH = 250;

const validateFields = (field: string, value: string): void => {
    const trimmedValue = value.trim();
    const errorMsg = getValidationError(field, trimmedValue);

    setErrors(prev => ({ ...prev, [field]: errorMsg }));
};

const getValidationError = (field: string, trimmedValue: string): string => {
    if (field === addBookFields.reason) {
        return validateReason(trimmedValue);
    }

    return validateTitleOrAuthor(field, trimmedValue);
};

const validateReason = (value: string): string => {
    if (value.length < MIN_REASON_LENGTH) {
        return placeHoldersError.reasonLeastValue;
    }

    if (value.length > MAX_REASON_LENGTH) {
        return placeHoldersError.reasonExceedValue;
    }

    return '';
};

const validateTitleOrAuthor = (field: string, value: string): string => {
    if (!value) {
        return getRequiredFieldError(field);
    }

    if (!regex.titleAuthorCategory.test(value)) {
        return `Invalid ${field}`;
    }

    return '';
};

const getRequiredFieldError = (field: string): string => {
    const fieldName = field === addBookFields.title ? 'Book title' : 'Author';
    return `${fieldName} is required`;
};

//My Code
enum StepVisualState {
  COMPLETE = 'step-complete',     // green
  INCOMPLETE = 'step-incomplete', // orange
  DISABLED = 'disabled'
}

interface StepRule {
  caseStatus?: number;
  caseStage?: number | number[];
  resolve: (index: number, totalSteps: number) => StepVisualState;
}

enum MonthEndClosePageEnum{
    Overview,InputData,Optimization,Review,Publish
}

enum MonthEndCloseStatusEnum{
    Started,InProgress,Complete,Failed,Deleted
}

interface WizardStep {
  label: string;
  disabled?: boolean;
  cssClass?: string;
}

function prettifyTitle(text:string):string{
    return text
}

const STEP_RULES:{
    status:Record<number,(index:number)=>StepVisualState>,
    stage:Record<number,(index:number)=>StepVisualState>
} = {
    status: {
        [MonthEndCloseStatusEnum.Complete]: () => StepVisualState.COMPLETE,
    },
    stage: {
        [MonthEndClosePageEnum.InputData]: (index:number) =>
            index === 0 ? StepVisualState.COMPLETE :
                index === 1 ? StepVisualState.INCOMPLETE :
                    StepVisualState.DISABLED,

        [MonthEndClosePageEnum.Optimization]: (index:number) =>
            index < 2 ? StepVisualState.COMPLETE :
                index === 2 ? StepVisualState.INCOMPLETE :
                    StepVisualState.DISABLED,

        [MonthEndClosePageEnum.Review]: (index:number) =>
            index <= 2 ? StepVisualState.COMPLETE :
                index === 3 ? StepVisualState.INCOMPLETE :
                    StepVisualState.DISABLED,

        [MonthEndClosePageEnum.Publish]: (index:number) =>
            index <= 2 ? StepVisualState.COMPLETE :
                index === 3 ? StepVisualState.INCOMPLETE :
                    StepVisualState.DISABLED
    }
};

class MonthEndCloseWizardComponent{
    steps:WizardStep[] = [
        { label: MonthEndClosePageEnum[MonthEndClosePageEnum.Overview]},
        { label: prettifyTitle(MonthEndClosePageEnum[MonthEndClosePageEnum.InputData]) },
        { label: MonthEndClosePageEnum[MonthEndClosePageEnum.Optimization]},
        { label: MonthEndClosePageEnum[MonthEndClosePageEnum.Review] }
    ];

    resolveStepState(index: number, caseStatus: number, caseStage: number) {
        const statusRule = STEP_RULES.status[caseStatus];
        if (statusRule) {
            return statusRule(index);
        }

        const stageRule = STEP_RULES.stage[caseStage];
        if (stageRule) {
            return stageRule(index);
        }

        return StepVisualState.DISABLED;
    }

    ngOnInit() {
        const { caseStage, caseStatus } = JSON.parse(sessionStorage.getItem("LastSelectedCase")!)
        this.steps = this.steps.map((step, index) => {
            const state:StepVisualState = this.resolveStepState(index, caseStage, caseStatus)
            const newStep: WizardStep={
                ...step,
                disabled: state === StepVisualState.DISABLED,
                cssClass:state
            }
            if(state===StepVisualState.DISABLED) delete newStep?.cssClass
            return newStep
        })
    }
}






