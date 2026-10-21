import { startPlayable } from '@engine/core/app';
import { runBeats } from '@engine/scenario/beats';
import { createBeats } from './game/beats';

startPlayable((ctx) => runBeats(ctx, createBeats(ctx)));
